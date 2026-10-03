using FixFlow.Api.Features.WorkOrders.ReassignTechnician;

namespace FixFlow.Api.UnitTests.Features.WorkOrders;

public sealed class ReassignTechnicianRequestValidatorTests
{
    private readonly ReassignTechnicianRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_Due_Date_Is_Omitted()
    {
        var result = _validator.Validate(new ReassignTechnicianRequest(Guid.CreateVersion7()));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Request_When_Technician_Id_Is_Empty()
    {
        var result = _validator.Validate(new ReassignTechnicianRequest(Guid.Empty, DateTimeOffset.UtcNow.AddDays(1)));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(ReassignTechnicianRequest.TechnicianId));
    }
}
