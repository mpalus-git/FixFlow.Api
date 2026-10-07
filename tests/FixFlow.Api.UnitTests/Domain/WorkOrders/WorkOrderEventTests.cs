using FixFlow.Api.Domain.Clients;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.UnitTests.Domain.WorkOrders;

public sealed class WorkOrderEventTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DueDate = Now.AddDays(2);
    private static readonly Guid DispatcherId = Guid.CreateVersion7();
    private static readonly Guid TechnicianId = Guid.CreateVersion7();
    private static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

    [Fact]
    public void Should_Record_Created_Event_With_Actor_When_Work_Order_Is_Created()
    {
        var workOrder = CreateWorkOrder();

        var created = workOrder.PendingEvents.ShouldHaveSingleItem();
        created.Type.ShouldBe(WorkOrderEventType.Created);
        created.WorkOrderId.ShouldBe(workOrder.Id);
        created.OccurredAt.ShouldBe(Now);
        created.ActorId.ShouldBe(DispatcherId);
        created.TechnicianId.ShouldBeNull();
        created.DueDate.ShouldBe(DueDate);
    }

    [Fact]
    public void Should_Record_Assigned_Event_With_Technician_And_New_Due_Date_When_Technician_Is_Assigned()
    {
        var workOrder = CreateWorkOrder();

        workOrder.Assign(TechnicianId, DueDate.AddDays(1), Now.AddHours(1), DispatcherId);

        var assigned = workOrder.PendingEvents[^1];
        assigned.Type.ShouldBe(WorkOrderEventType.Assigned);
        assigned.OccurredAt.ShouldBe(Now.AddHours(1));
        assigned.ActorId.ShouldBe(DispatcherId);
        assigned.TechnicianId.ShouldBe(TechnicianId);
        assigned.DueDate.ShouldBe(DueDate.AddDays(1));
    }

    [Fact]
    public void Should_Record_Reassigned_Event_With_New_Technician_When_Work_Order_Is_Reassigned()
    {
        var workOrder = CreateAssignedWorkOrder();
        var otherTechnicianId = Guid.CreateVersion7();

        workOrder.Reassign(otherTechnicianId, null, Now.AddHours(2), DispatcherId);

        var reassigned = workOrder.PendingEvents[^1];
        reassigned.Type.ShouldBe(WorkOrderEventType.Reassigned);
        reassigned.TechnicianId.ShouldBe(otherTechnicianId);
        reassigned.ActorId.ShouldBe(DispatcherId);
    }

    [Fact]
    public void Should_Record_Unassigned_Event_Without_Technician_When_Technician_Is_Unassigned()
    {
        var workOrder = CreateAssignedWorkOrder();

        workOrder.Unassign(Now.AddHours(2), DispatcherId);

        var unassigned = workOrder.PendingEvents[^1];
        unassigned.Type.ShouldBe(WorkOrderEventType.Unassigned);
        unassigned.OccurredAt.ShouldBe(Now.AddHours(2));
        unassigned.TechnicianId.ShouldBeNull();
    }

    [Fact]
    public void Should_Record_Started_Event_At_Start_Time_By_Technician_When_Work_Is_Started()
    {
        var workOrder = CreateAssignedWorkOrder();

        workOrder.Start(TechnicianId, technicianHasWorkInProgress: false, Now.AddHours(3), Now.AddHours(2), MaxClockSkew);

        var started = workOrder.PendingEvents[^1];
        started.Type.ShouldBe(WorkOrderEventType.Started);
        started.OccurredAt.ShouldBe(Now.AddHours(2));
        started.ActorId.ShouldBe(TechnicianId);
    }

    [Fact]
    public void Should_Record_Completed_And_Invoiced_Events_When_Work_Order_Is_Closed()
    {
        var workOrder = CreateAssignedWorkOrder();
        workOrder.Start(TechnicianId, technicianHasWorkInProgress: false, Now.AddHours(2));

        workOrder.Complete(hasServiceEntries: true, Now.AddHours(4), actorId: TechnicianId);
        workOrder.Invoice(Now.AddDays(1), DispatcherId);

        workOrder.PendingEvents.Select(item => item.Type).ShouldBe(
        [
            WorkOrderEventType.Created,
            WorkOrderEventType.Assigned,
            WorkOrderEventType.Started,
            WorkOrderEventType.Completed,
            WorkOrderEventType.Invoiced,
        ]);
        workOrder.PendingEvents[^2].ActorId.ShouldBe(TechnicianId);
        workOrder.PendingEvents[^1].ActorId.ShouldBe(DispatcherId);
    }

    [Fact]
    public void Should_Record_Updated_Event_When_Work_Order_Is_Edited()
    {
        var workOrder = CreateWorkOrder();

        workOrder.Update("Leak and noisy fan", WorkOrderPriority.High, DueDate.AddDays(3), Now.AddHours(1), DispatcherId);

        var updated = workOrder.PendingEvents[^1];
        updated.Type.ShouldBe(WorkOrderEventType.Updated);
        updated.DueDate.ShouldBe(DueDate.AddDays(3));
    }

    [Fact]
    public void Should_Not_Record_Event_When_Transition_Is_Rejected_Or_Retried()
    {
        var workOrder = CreateAssignedWorkOrder();
        workOrder.Start(TechnicianId, technicianHasWorkInProgress: false, Now.AddHours(2));
        var eventCount = workOrder.PendingEvents.Count;

        workOrder.Start(TechnicianId, technicianHasWorkInProgress: false, Now.AddHours(3));
        workOrder.Invoice(Now.AddHours(3), DispatcherId);
        workOrder.Complete(hasServiceEntries: false, Now.AddHours(3), actorId: TechnicianId);

        workOrder.PendingEvents.Count.ShouldBe(eventCount);
    }

    [Fact]
    public void Should_Clear_Pending_Events_When_They_Are_Taken()
    {
        var workOrder = CreateAssignedWorkOrder();

        var events = workOrder.TakePendingEvents();

        events.Select(item => item.Type).ShouldBe([WorkOrderEventType.Created, WorkOrderEventType.Assigned]);
        workOrder.PendingEvents.ShouldBeEmpty();
    }

    private static WorkOrder CreateWorkOrder()
    {
        var client = Client.Create("Klimat-Serwis", new Address("Marszałkowska", "10A", "00-590", "Warszawa"), "Anna Nowak", "+48 600 100 200", null, Now);
        var device = Device.Create(client, "AC-1001", "Split 3.5 kW", "Daikin", new DateOnly(2024, 5, 20), Now).Value;
        return WorkOrder.Create(device, "Air conditioner is leaking", WorkOrderPriority.Normal, DueDate, Now, DispatcherId).Value;
    }

    private static WorkOrder CreateAssignedWorkOrder()
    {
        var workOrder = CreateWorkOrder();
        workOrder.Assign(TechnicianId, null, Now.AddHours(1), DispatcherId);
        return workOrder;
    }
}
