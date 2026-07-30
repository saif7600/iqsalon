using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AtiqSalon.Api.Data;
using AtiqSalon.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AtiqSalon.Api.Application;

public sealed class IqaiPortalService(
    HttpClient http,
    IConfiguration configuration,
    TenantContext tenant,
    AppDbContext db)
{
    public bool IsConfigured => Uri.TryCreate(configuration["IQAI_BASE_URL"], UriKind.Absolute, out _)
        && !string.IsNullOrWhiteSpace(configuration["IQAI_SDK_TOKEN"]);

    public async Task<IqaiPortalResponse> AskAsync(IqaiPortalRequest request, CancellationToken ct)
    {
        if (!IsConfigured) throw new InvalidOperationException("IQAI is not configured.");
        if (tenant.TenantId is null || tenant.UserId is null) throw new UnauthorizedAccessException();
        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 8000)
            throw new ArgumentException("A message between 1 and 8000 characters is required.");
        var baseUrl = (configuration["IQAI_INTERNAL_BASE_URL"]
            ?? configuration["IQAI_BASE_URL"])!.TrimEnd('/');
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/sdk/v1/chat/completions");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration["IQAI_SDK_TOKEN"]);
        message.Headers.Add("X-AtiqSalon-Tenant-Id", tenant.TenantId.Value.ToString());
        message.Headers.Add("X-AtiqSalon-User-Id", tenant.UserId.Value.ToString());
        var facts = await GetBusinessFactsAsync(ct);
        var prompt = $"""
            You are IQAI, the business copilot inside AtiqSalon.
            Answer using the supplied tenant facts. Never invent bookings, customers,
            staff, services, or branches. If the facts do not contain the requested
            metric, say that it is not available. Give concise operational advice
            after the factual answer. Never claim to have changed a record.
            Language: {request.LanguageCode ?? "en"}.

            Current tenant facts (UTC):
            {JsonSerializer.Serialize(facts)}

            User request:
            {request.Message.Trim()}
            """;
        message.Content = JsonContent.Create(new
        {
            model = "gemini-3.1-flash-lite",
            messages = new[] { new { role = "user", content = prompt } },
            stream = false
        });
        using var response = await http.SendAsync(message, ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"IQAI returned HTTP {(int)response.StatusCode}.");
        var payload = await response.Content.ReadFromJsonAsync<IqaiSdkResponse>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web), ct)
            ?? throw new InvalidOperationException("IQAI returned an empty response.");
        var text = payload.Choices.FirstOrDefault()?.Message.Content;
        if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("IQAI returned no answer.");
        return new IqaiPortalResponse(request.ConversationId ?? $"atiqsalon-{Guid.CreateVersion7():N}",
            text, [], []);
    }

    private async Task<object> GetBusinessFactsAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var weekEnd = now.AddDays(7);
        var appointments = db.Appointments.Where(x => x.StartAtUtc >= now && x.StartAtUtc < weekEnd);
        return new
        {
            generatedAtUtc = now,
            nextSevenDays = new
            {
                appointments = await appointments.CountAsync(ct),
                confirmed = await appointments.CountAsync(x => x.Status == "Confirmed", ct),
                checkedIn = await appointments.CountAsync(x => x.Status == "CheckedIn", ct),
                inProgress = await appointments.CountAsync(x => x.Status == "InProgress", ct),
                cancelled = await appointments.CountAsync(x => x.Status == "Cancelled", ct)
            },
            totals = new
            {
                branches = await db.Branches.CountAsync(ct),
                customers = await db.Customers.CountAsync(ct),
                staff = await db.StaffMembers.CountAsync(ct),
                services = await db.SalonServices.CountAsync(ct)
            }
        };
    }

    private sealed record IqaiSdkResponse(IReadOnlyList<IqaiChoice> Choices);
    private sealed record IqaiChoice(IqaiMessage Message);
    private sealed record IqaiMessage(string Content);
}

public sealed record IqaiPortalRequest(string Message, string? ConversationId = null,
    string? LanguageCode = null);
public sealed record IqaiPortalResponse(string ConversationId, string Text,
    IReadOnlyList<JsonElement> Sources, IReadOnlyList<string> SuggestedActions);

public static class IqaiPortalEndpoints
{
    public static IEndpointRouteBuilder MapIqaiPortalApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1/iqai");
        api.MapGet("/status", (IqaiPortalService service) => Results.Ok(new
        {
            provider = "IQAI",
            configured = service.IsConfigured,
            mode = "advisory",
            writesEnabled = false
        })).RequireAuthorization("iqai.use");
        api.MapPost("/chat", async (IqaiPortalRequest request, IqaiPortalService service,
            CancellationToken ct) =>
        {
            try { return Results.Ok(await service.AskAsync(request, ct)); }
            catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
            catch (InvalidOperationException)
            {
                return Results.Problem(
                    statusCode: 503, title: "IQAI unavailable", detail: "IQAI is not configured or temporarily unavailable.");
            }
            catch (TaskCanceledException)
            {
                return Results.Problem(
                    statusCode: 503, title: "IQAI timed out", detail: "IQAI did not respond in time. Try a shorter request.");
            }
            catch (HttpRequestException)
            {
                return Results.Problem(
                    statusCode: 503, title: "IQAI unavailable", detail: "IQAI could not be reached.");
            }
        }).RequireAuthorization("iqai.use");
        return endpoints;
    }
}
