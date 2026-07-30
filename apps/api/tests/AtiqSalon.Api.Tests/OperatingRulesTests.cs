using AtiqSalon.Api.Application;
using AtiqSalon.Api.Domain;
using AtiqSalon.Api.Security;
namespace AtiqSalon.Api.Tests;

public sealed class OperatingRulesTests
{
    [Theory]
    [InlineData("Draft", "Confirmed", true)]
    [InlineData("Confirmed", "CheckedIn", true)]
    [InlineData("InProgress", "Completed", true)]
    [InlineData("Completed", "InProgress", false)]
    [InlineData("Cancelled", "CheckedIn", false)]
    public void Appointment_transitions_are_explicit(string current, string next, bool expected) => Assert.Equal(expected, AppointmentLifecycle.CanTransition(current, next));
    [Fact]
    public void Service_validation_rejects_invalid_duration_and_deposit() { var errors = ServiceRules.Validate(new SalonService { DurationMinutes = 0, BasePrice = 100, DepositType = "Percentage", DepositValue = 101 }); Assert.Contains("durationMinutes", errors.Keys); Assert.Contains("depositValue", errors.Keys); }
    [Fact]
    public void Receptionist_does_not_receive_sensitive_note_permission() => Assert.DoesNotContain("customers.notes.sensitive.read", PermissionCatalog.ForRoles(["Receptionist"]));
    [Theory]
    [InlineData(1, 0, 1, true)]
    [InlineData(1, 1, 1, false)]
    [InlineData(3, 1, 2, true)]
    [InlineData(3, 2, 2, false)]
    public void Resource_capacity_is_enforced(int capacity, int reserved, int requested, bool expected) =>
        Assert.Equal(expected, BookingRules.HasResourceCapacity(capacity, reserved, requested));
    [Fact]
    public async Task Concurrent_capacity_one_reservations_allow_exactly_one_winner()
    {
        var reserved = 0;
        var gate = new SemaphoreSlim(1, 1);
        async Task<bool> Reserve()
        {
            await gate.WaitAsync();
            try
            {
                if (!BookingRules.HasResourceCapacity(1, reserved, 1)) return false;
                reserved++;
                return true;
            }
            finally { gate.Release(); }
        }
        var results = await Task.WhenAll(Reserve(), Reserve());
        Assert.Single(results, x => x);
    }
}
