using FixFlow.Api.Domain.Users;

namespace FixFlow.Api.UnitTests.Domain.Users;

public sealed class ApplicationUserTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Should_Be_Active_When_Created()
    {
        new ApplicationUser().IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Should_Keep_First_Deactivation_Time_When_Deactivated_Twice()
    {
        var user = new ApplicationUser();

        user.Deactivate(Now);
        user.Deactivate(Now.AddDays(1));

        user.IsActive.ShouldBeFalse();
        user.DeactivatedAt.ShouldBe(Now);
    }

    [Fact]
    public void Should_Be_Active_Again_When_Deactivated_User_Is_Activated()
    {
        var user = new ApplicationUser();
        user.Deactivate(Now);

        user.Activate();

        user.IsActive.ShouldBeTrue();
        user.DeactivatedAt.ShouldBeNull();
    }
}
