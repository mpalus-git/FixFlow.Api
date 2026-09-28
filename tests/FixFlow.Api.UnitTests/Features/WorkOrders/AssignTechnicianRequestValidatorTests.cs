using FixFlow.Api.Features.WorkOrders.AssignTechnician;

namespace FixFlow.Api.UnitTests.Features.WorkOrders;

public sealed class AssignTechnicianRequestValidatorTests
{
    private readonly AssignTechnicianRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Request_When_Technician_Id_Is_Set()
    {
        var result = _validator.Validate(new AssignTechnicianRequest(Guid.CreateVersion7()));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Request_When_Technician_Id_Is_Empty()
    {
        var result = _validator.Validate(new AssignTechnicianRequest(Guid.Empty));

        result.Errors.ShouldContain(error => error.PropertyName == nameof(AssignTechnicianRequest.TechnicianId));
    }
}
