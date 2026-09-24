using FixFlow.Api.Domain.Clients;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.Parts;
using FixFlow.Api.Domain.ServiceEntries;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.UnitTests.Domain.ServiceEntries;

public sealed class ServiceEntryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid TechnicianId = Guid.CreateVersion7();

    [Fact]
    public void Should_Return_Parts_To_Stock_At_Last_Used_Price_When_Correction_Does_Not_Exceed_Net_Consumption()
    {
        var workOrder = CreateInProgressWorkOrder();
        var part = CreatePart(stockQuantity: 10, unitPrice: 40m);
        var firstEntry = CreateWorkEntry(workOrder, new PartUsage(part, 2), Now.AddHours(1));
        part.Update(part.Name, part.CatalogNumber, 45m);
        var secondEntry = CreateWorkEntry(workOrder, new PartUsage(part, 3), Now.AddHours(2));

        var result = ServiceEntry.CreateCorrection(workOrder, TechnicianId, "Two filters were not used", [], [new PartUsage(part, 2)], [secondEntry, firstEntry], Now.AddHours(3));

        result.IsError.ShouldBeFalse();
        var correction = result.Value;
        correction.IsCorrection.ShouldBeTrue();
        correction.WorkStartedAt.ShouldBeNull();
        correction.Latitude.ShouldBeNull();
        correction.Parts.ShouldHaveSingleItem().ShouldBeEquivalentTo(new ServiceEntryPart(part.Id, 2, 45m));
        part.StockQuantity.ShouldBe(7);
    }

    [Fact]
    public void Should_Reject_Correction_When_Returned_Quantity_Exceeds_Net_Consumption_After_Earlier_Returns()
    {
        var workOrder = CreateInProgressWorkOrder();
        var part = CreatePart(stockQuantity: 10);
        var workEntry = CreateWorkEntry(workOrder, new PartUsage(part, 3), Now.AddHours(1));
        var earlierCorrection = ServiceEntry.CreateCorrection(workOrder, TechnicianId, "One unused", [], [new PartUsage(part, 1)], [workEntry], Now.AddHours(2)).Value;

        var result = ServiceEntry.CreateCorrection(workOrder, TechnicianId, "Three unused", [], [new PartUsage(part, 3)], [workEntry, earlierCorrection], Now.AddHours(3));

        result.FirstError.ShouldBe(ServiceEntryErrors.ReturnExceedsConsumption);
        part.StockQuantity.ShouldBe(8);
    }

    [Fact]
    public void Should_Reject_Correction_When_Part_Was_Not_Used_On_Work_Order()
    {
        var workOrder = CreateInProgressWorkOrder();
        var otherWorkOrder = CreateInProgressWorkOrder();
        var part = CreatePart(stockQuantity: 10);
        var entryOfOtherWorkOrder = CreateWorkEntry(otherWorkOrder, new PartUsage(part, 3), Now.AddHours(1));

        var result = ServiceEntry.CreateCorrection(workOrder, TechnicianId, "Unused", [], [new PartUsage(part, 1)], [entryOfOtherWorkOrder], Now.AddHours(2));

        result.FirstError.ShouldBe(ServiceEntryErrors.ReturnExceedsConsumption);
        part.StockQuantity.ShouldBe(7);
    }

    [Fact]
    public void Should_Return_Parts_To_Stock_When_Part_Was_Archived_After_Use()
    {
        var workOrder = CreateInProgressWorkOrder();
        var part = CreatePart(stockQuantity: 10);
        var workEntry = CreateWorkEntry(workOrder, new PartUsage(part, 3), Now.AddHours(1));
        part.Archive(Now.AddHours(2));

        var result = ServiceEntry.CreateCorrection(workOrder, TechnicianId, "Unused", [], [new PartUsage(part, 3)], [workEntry], Now.AddHours(3));

        result.IsError.ShouldBeFalse();
        part.StockQuantity.ShouldBe(10);
    }

    [Fact]
    public void Should_Reject_Correction_When_Technician_Is_Not_Assigned_To_Work_Order()
    {
        var workOrder = CreateInProgressWorkOrder();
        var part = CreatePart(stockQuantity: 10);
        var workEntry = CreateWorkEntry(workOrder, new PartUsage(part, 3), Now.AddHours(1));

        var result = ServiceEntry.CreateCorrection(workOrder, Guid.CreateVersion7(), "Unused", [], [new PartUsage(part, 1)], [workEntry], Now.AddHours(2));

        result.FirstError.ShouldBe(WorkOrderErrors.NotAssignedToTechnician);
    }

    private static Part CreatePart(int stockQuantity, decimal unitPrice = 49.99m) =>
        Part.Create("Filtr powietrza", "FLT-100", stockQuantity, unitPrice, Now);

    private static ServiceEntry CreateWorkEntry(WorkOrder workOrder, PartUsage usage, DateTimeOffset finishedAt) =>
        ServiceEntry.CreateWork(workOrder, TechnicianId, "Replaced filters", [], finishedAt.AddMinutes(-30), finishedAt, null, [usage], finishedAt).Value;

    private static WorkOrder CreateInProgressWorkOrder()
    {
        var client = Client.Create("Klimat-Serwis", new Address("Marszałkowska", "10A", "00-590", "Warszawa"), "Anna Nowak", "+48 600 100 200", null, Now);
        var device = Device.Create(client, "AC-1001", "Split 3.5 kW", "Daikin", new DateOnly(2024, 5, 20), Now).Value;
        var workOrder = WorkOrder.Create(device, "Air conditioner is leaking", WorkOrderPriority.Normal, Now.AddDays(2), Now).Value;
        workOrder.Assign(TechnicianId);
        workOrder.Start(TechnicianId, technicianHasWorkInProgress: false, Now);
        return workOrder;
    }
}
