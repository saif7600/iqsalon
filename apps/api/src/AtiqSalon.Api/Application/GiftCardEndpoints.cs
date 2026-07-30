namespace AtiqSalon.Api.Application;

public static class GiftCardEndpoints
{
    public static IEndpointRouteBuilder MapGiftCardApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1");
        api.MapPost("/gift-cards", async (IssueGiftCardRequest request,
            GiftCardService service, CancellationToken ct) =>
        {
            var result = await service.Issue(request, ct);
            return result.IsSuccess ? Results.Created($"/api/v1/gift-cards/{result.Id}", result) : Results.Conflict(result);
        }).RequireAuthorization("gift_cards.issue");
        api.MapPost("/gift-cards/redeem", async (RedeemGiftCardRequest request,
            GiftCardService service, CancellationToken ct) =>
        {
            var result = await service.Redeem(request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.Conflict(result);
        }).RequireAuthorization("gift_cards.redeem");
        api.MapPost("/gift-cards/balance", async (GiftCardBalanceRequest request,
            GiftCardService service, CancellationToken ct) =>
            await service.GetBalance(request.Code, ct) is { } result
                ? Results.Ok(result)
                : Results.NotFound()).RequireAuthorization("gift_cards.read");
        return endpoints;
    }
}

public sealed record GiftCardBalanceRequest(string Code);
