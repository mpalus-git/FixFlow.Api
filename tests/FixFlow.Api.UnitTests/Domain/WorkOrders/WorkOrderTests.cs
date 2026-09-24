using FixFlow.Api.Domain.Clients;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.UnitTests.Domain.WorkOrders;

public sealed class WorkOrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DueDate = Now.AddDays(2);
    private static readonly Guid TechnicianId = Guid.CreateVersion7();

    [Fact]
    public void Should_Create_New_Unassigned_Work_Order_When_Device_Is_Active_And_Due_Date_Is_In_Future()
    {
        var device = CreateDevice();

        var result = WorkOrder.Create(device, "Air conditioner is leaking", WorkOrderPriority.High, DueDate, Now);

        result.IsError.ShouldBeFalse();
        var workOrder = result.Value;
        workOrder.Id.ShouldNotBe(Guid.Empty);
        workOrder.DeviceId.ShouldBe(device.Id);
        workOrder.Priority.ShouldBe(WorkOrderPriority.High);
        workOrder.DueDate.ShouldBe(DueDate);
        workOrder.Status.ShouldBe(WorkOrderStatus.New);
        workOrder.TechnicianId.ShouldBeNull();
        workOrder.CreatedAt.ShouldBe(Now);
    }

    [Fact]
    public void Should_Reject_Creating_Work_Order_When_Device_Is_Archived()
    {
        var device = CreateDevice();
        device.Archive(Now);

        var result = WorkOrder.Create(device, "Air conditioner is leaking", WorkOrderPriority.Normal, DueDate, Now);

        result.FirstError.ShouldBe(WorkOrderErrors.DeviceArchived);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Reject_Creating_Work_Order_When_Due_Date_Is_Not_In_Future(int minutesFromNow)
    {
        var result = WorkOrder.Create(CreateDevice(), "Air conditioner is leaking", WorkOrderPriority.Normal, Now.AddMinutes(minutesFromNow), Now);

        result.FirstError.ShouldBe(WorkOrderErrors.DueDateNotInFuture);
    }

    [Fact]
    public void Should_Change_Details_When_In_Progress_Work_Order_Is_Updated()
    {
        var workOrder = CreateInProgressWorkOrder();

        var result = workOrder.Update("Leak and noisy fan", WorkOrderPriority.Critical, DueDate.AddDays(1), Now.AddHours(1));

        result.IsError.ShouldBeFalse();
        workOrder.Description.ShouldBe("Leak and noisy fan");
        workOrder.Priority.ShouldBe(WorkOrderPriority.Critical);
        workOrder.DueDate.ShouldBe(DueDate.AddDays(1));
        workOrder.Status.ShouldBe(WorkOrderStatus.InProgress);
    }

    [Fact]
    public void Should_Accept_Unchanged_Past_Due_Date_When_Overdue_Work_Order_Is_Updated()
    {
        var workOrder = CreateWorkOrder();

        var result = workOrder.Update("Air conditioner is leaking", WorkOrderPriority.Critical, DueDate, DueDate.AddDays(1));

        result.IsError.ShouldBeFalse();
        workOrder.Priority.ShouldBe(WorkOrderPriority.Critical);
    }

    [Fact]
    public void Should_Reject_Update_When_Changed_Due_Date_Is_Not_In_Future()
    {
        var workOrder = CreateWorkOrder();

        var result = workOrder.Update("Air conditioner is leaking", WorkOrderPriority.Low, Now.AddHours(1), Now.AddHours(2));

        result.FirstError.ShouldBe(WorkOrderErrors.DueDateNotInFuture);
        workOrder.DueDate.ShouldBe(DueDate);
        workOrder.Priority.ShouldBe(WorkOrderPriority.Normal);
    }

    [Fact]
    public void Should_Assign_Technician_When_Work_Order_Is_New()
    {
        var workOrder = CreateWorkOrder();

        var result = workOrder.Assign(TechnicianId);

        result.IsError.ShouldBeFalse();
        workOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
        workOrder.TechnicianId.ShouldBe(TechnicianId);
    }

    [Fact]
    public void Should_Reject_Assigning_Technician_When_Work_Order_Is_Already_Assigned()
    {
        var workOrder = CreateAssignedWorkOrder();

        var result = workOrder.Assign(Guid.CreateVersion7());

        result.FirstError.Code.ShouldBe("WorkOrder.InvalidStatusTransition");
        workOrder.TechnicianId.ShouldBe(TechnicianId);
    }

    [Fact]
    public void Should_Return_To_New_Without_Technician_When_Assigned_Work_Order_Is_Unassigned()
    {
        var workOrder = CreateAssignedWorkOrder();

        var result = workOrder.Unassign();

        result.IsError.ShouldBeFalse();
        workOrder.Status.ShouldBe(WorkOrderStatus.New);
        workOrder.TechnicianId.ShouldBeNull();
    }

    [Fact]
    public void Should_Reject_Unassigning_When_Work_Order_Is_New()
    {
        var result = CreateWorkOrder().Unassign();

        result.FirstError.Code.ShouldBe("WorkOrder.InvalidStatusTransition");
    }

    [Fact]
    public void Should_Reject_Unassigning_When_Work_Order_Is_In_Progress()
    {
        var workOrder = CreateInProgressWorkOrder();

        var result = workOrder.Unassign();

        result.FirstError.Code.ShouldBe("WorkOrder.InvalidStatusTransition");
        workOrder.Status.ShouldBe(WorkOrderStatus.InProgress);
        workOrder.TechnicianId.ShouldBe(TechnicianId);
    }

    [Fact]
    public void Should_Start_Work_When_Assigned_Technician_Has_No_Work_In_Progress()
    {
        var workOrder = CreateAssignedWorkOrder();

        var result = workOrder.Start(TechnicianId, technicianHasWorkInProgress: false, Now.AddHours(1));

        result.IsError.ShouldBeFalse();
        workOrder.Status.ShouldBe(WorkOrderStatus.InProgress);
        workOrder.StartedAt.ShouldBe(Now.AddHours(1));
    }

    [Fact]
    public void Should_Reject_Starting_Work_When_Technician_Already_Has_Work_In_Progress()
    {
        var workOrder = CreateAssignedWorkOrder();

        var result = workOrder.Start(TechnicianId, technicianHasWorkInProgress: true, Now);

        result.FirstError.ShouldBe(WorkOrderErrors.TechnicianAlreadyHasWorkInProgress);
        workOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Fact]
    public void Should_Reject_Starting_Work_When_Work_Order_Is_Assigned_To_Another_Technician()
    {
        var workOrder = CreateAssignedWorkOrder();

        var result = workOrder.Start(Guid.CreateVersion7(), technicianHasWorkInProgress: false, Now);

        result.FirstError.ShouldBe(WorkOrderErrors.NotAssignedToTechnician);
    }

    [Fact]
    public void Should_Reject_Starting_Work_When_Work_Order_Is_New()
    {
        var result = CreateWorkOrder().Start(TechnicianId, technicianHasWorkInProgress: false, Now);

        result.FirstError.Code.ShouldBe("WorkOrder.InvalidStatusTransition");
    }

    [Fact]
    public void Should_Reject_Starting_Work_When_Work_Order_Is_Already_In_Progress()
    {
        var workOrder = CreateInProgressWorkOrder();

        var result = workOrder.Start(TechnicianId, technicianHasWorkInProgress: false, Now.AddHours(2));

        result.FirstError.Code.ShouldBe("WorkOrder.InvalidStatusTransition");
        workOrder.StartedAt.ShouldBe(Now);
    }

    private static Device CreateDevice()
    {
        var client = Client.Create("Klimat-Serwis", new Address("Marszałkowska", "10A", "00-590", "Warszawa"), "Anna Nowak", "+48 600 100 200", null, Now);
        return Device.Create(client, "AC-1001", "Split 3.5 kW", "Daikin", new DateOnly(2024, 5, 20), Now).Value;
    }

    private static WorkOrder CreateWorkOrder() =>
        WorkOrder.Create(CreateDevice(), "Air conditioner is leaking", WorkOrderPriority.Normal, DueDate, Now).Value;

    private static WorkOrder CreateAssignedWorkOrder()
    {
        var workOrder = CreateWorkOrder();
        workOrder.Assign(TechnicianId);
        return workOrder;
    }

    private static WorkOrder CreateInProgressWorkOrder()
    {
        var workOrder = CreateAssignedWorkOrder();
        workOrder.Start(TechnicianId, technicianHasWorkInProgress: false, Now);
        return workOrder;
    }
}
