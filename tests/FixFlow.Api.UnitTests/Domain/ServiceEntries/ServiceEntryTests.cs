using ErrorOr;
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
    public void Should_Create_Work_Entry_And_Consume_Parts_At_Current_Price_When_Work_Order_Is_In_Progress()
    {
        var workOrder = CreateInProgressWorkOrder();
        var part = CreatePart(stockQuantity: 10, unitPrice: 40m);
        var location = new GpsLocation(52.2297, 21.0122);

        var result = ServiceEntry.CreateWork(
            workOrder,
            TechnicianId,
            "Replaced filters",
            ["https://photos.test/1.jpg"],
            Now.AddMinutes(10),
            Now.AddMinutes(70),
            location,
            [new PartUsage(part, 4)],
            Now.AddHours(2));

        result.IsError.ShouldBeFalse();
        var entry = result.Value;
        entry.WorkOrderId.ShouldBe(workOrder.Id);
        entry.TechnicianId.ShouldBe(TechnicianId);
        entry.IsCorrection.ShouldBeFalse();
        entry.PhotoUrls.ShouldBe(["https://photos.test/1.jpg"]);
        entry.WorkStartedAt.ShouldBe(Now.AddMinutes(10));
        entry.WorkFinishedAt.ShouldBe(Now.AddMinutes(70));
        entry.Latitude.ShouldBe(location.Latitude);
        entry.Longitude.ShouldBe(location.Longitude);
        entry.CreatedAt.ShouldBe(Now.AddHours(2));
        entry.Parts.ShouldHaveSingleItem().ShouldBeEquivalentTo(new ServiceEntryPart(part.Id, 4, 40m));
        part.StockQuantity.ShouldBe(6);
    }

    [Fact]
    public void Should_Reject_Work_Entry_When_Part_Consumption_Would_Drop_Stock_Below_Zero()
    {
        var part = CreatePart(stockQuantity: 2);

        var result = ServiceEntry.CreateWork(CreateInProgressWorkOrder(), TechnicianId, "Replaced filters", [], Now, Now.AddHours(1), null, [new PartUsage(part, 3)], Now.AddHours(1));

        result.FirstError.ShouldBe(PartErrors.InsufficientStock);
        part.StockQuantity.ShouldBe(2);
    }

    [Fact]
    public void Should_Reject_Work_Entry_When_Part_Is_Archived()
    {
        var part = CreatePart(stockQuantity: 5);
        part.Archive(Now);

        var result = ServiceEntry.CreateWork(CreateInProgressWorkOrder(), TechnicianId, "Replaced filters", [], Now, Now.AddHours(1), null, [new PartUsage(part, 1)], Now.AddHours(1));

        result.FirstError.ShouldBe(PartErrors.Archived);
    }

    [Fact]
    public void Should_Reject_Work_Entry_When_Work_Started_Before_Work_Order_Was_Started()
    {
        var result = CreateWorkEntryWithTime(Now.AddMinutes(-1), Now.AddMinutes(60), Now.AddMinutes(120));

        result.FirstError.ShouldBe(ServiceEntryErrors.WorkStartedBeforeWorkOrder);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(20)]
    public void Should_Reject_Work_Entry_When_Work_Does_Not_Finish_After_It_Starts(int finishMinutes)
    {
        var result = CreateWorkEntryWithTime(Now.AddMinutes(30), Now.AddMinutes(finishMinutes), Now.AddMinutes(120));

        result.FirstError.ShouldBe(ServiceEntryErrors.WorkNotFinishedAfterStart);
    }

    [Fact]
    public void Should_Reject_Work_Entry_When_Work_Finishes_In_The_Future()
    {
        var result = CreateWorkEntryWithTime(Now.AddMinutes(30), Now.AddMinutes(121), Now.AddMinutes(120));

        result.FirstError.ShouldBe(ServiceEntryErrors.WorkFinishedInFuture);
    }

    [Fact]
    public void Should_Reject_Work_Entry_When_Work_Order_Is_Not_In_Progress()
    {
        var assignedWorkOrder = WorkOrder.Create(CreateDevice(), "Noisy fan", WorkOrderPriority.Low, Now.AddDays(2), Now).Value;
        assignedWorkOrder.Assign(TechnicianId);

        var result = ServiceEntry.CreateWork(assignedWorkOrder, TechnicianId, "Replaced filters", [], Now, Now.AddHours(1), null, [], Now.AddHours(1));

        result.FirstError.ShouldBe(WorkOrderErrors.NotInProgress);
    }

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

    private static ErrorOr<ServiceEntry> CreateWorkEntryWithTime(DateTimeOffset startedAt, DateTimeOffset finishedAt, DateTimeOffset now) =>
        ServiceEntry.CreateWork(CreateInProgressWorkOrder(), TechnicianId, "Replaced filters", [], startedAt, finishedAt, null, [], now);

    private static Device CreateDevice()
    {
        var client = Client.Create("Klimat-Serwis", new Address("Marszałkowska", "10A", "00-590", "Warszawa"), "Anna Nowak", "+48 600 100 200", null, Now);
        return Device.Create(client, "AC-1001", "Split 3.5 kW", "Daikin", new DateOnly(2024, 5, 20), Now).Value;
    }

    private static WorkOrder CreateInProgressWorkOrder()
    {
        var workOrder = WorkOrder.Create(CreateDevice(), "Air conditioner is leaking", WorkOrderPriority.Normal, Now.AddDays(2), Now).Value;
        workOrder.Assign(TechnicianId);
        workOrder.Start(TechnicianId, technicianHasWorkInProgress: false, Now);
        return workOrder;
    }
}
