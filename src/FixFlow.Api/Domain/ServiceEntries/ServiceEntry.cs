using ErrorOr;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Domain.ServiceEntries;

public sealed class ServiceEntry
{
    private readonly List<ServiceEntryPart> _parts = [];

    private ServiceEntry()
    {
    }

    public Guid Id { get; private set; }

    public Guid WorkOrderId { get; private set; }

    public Guid TechnicianId { get; private set; }

    public string Note { get; private set; } = string.Empty;

    public bool IsCorrection { get; private set; }

    public IReadOnlyList<string> PhotoUrls { get; private set; } = [];

    public DateTimeOffset? WorkStartedAt { get; private set; }

    public DateTimeOffset? WorkFinishedAt { get; private set; }

    public double? Latitude { get; private set; }

    public double? Longitude { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<ServiceEntryPart> Parts => _parts;

    public static ErrorOr<ServiceEntry> CreateWork(
        WorkOrder workOrder,
        Guid technicianId,
        string note,
        IReadOnlyList<string> photoUrls,
        DateTimeOffset workStartedAt,
        DateTimeOffset workFinishedAt,
        GpsLocation? startLocation,
        IReadOnlyList<PartUsage> usedParts,
        DateTimeOffset now)
    {
        var acceptance = workOrder.EnsureCanAddServiceEntry(technicianId);
        if (acceptance.IsError)
        {
            return acceptance.Errors;
        }

        if (workStartedAt < workOrder.StartedAt)
        {
            return ServiceEntryErrors.WorkStartedBeforeWorkOrder;
        }

        if (workFinishedAt <= workStartedAt)
        {
            return ServiceEntryErrors.WorkNotFinishedAfterStart;
        }

        if (workFinishedAt > now)
        {
            return ServiceEntryErrors.WorkFinishedInFuture;
        }

        var entry = new ServiceEntry
        {
            Id = Guid.CreateVersion7(),
            WorkOrderId = workOrder.Id,
            TechnicianId = technicianId,
            Note = note,
            PhotoUrls = [.. photoUrls],
            WorkStartedAt = workStartedAt,
            WorkFinishedAt = workFinishedAt,
            Latitude = startLocation?.Latitude,
            Longitude = startLocation?.Longitude,
            CreatedAt = now,
        };

        foreach (var usage in usedParts)
        {
            var consumption = usage.Part.Consume(usage.Quantity);
            if (consumption.IsError)
            {
                return consumption.Errors;
            }

            entry._parts.Add(new ServiceEntryPart(usage.Part.Id, usage.Quantity, usage.Part.UnitPrice));
        }

        return entry;
    }

    public static ErrorOr<ServiceEntry> CreateCorrection(
        WorkOrder workOrder,
        Guid technicianId,
        string note,
        IReadOnlyList<string> photoUrls,
        IReadOnlyList<PartUsage> returnedParts,
        IReadOnlyCollection<ServiceEntry> previousEntries,
        DateTimeOffset now)
    {
        var acceptance = workOrder.EnsureCanAddServiceEntry(technicianId);
        if (acceptance.IsError)
        {
            return acceptance.Errors;
        }

        var entry = new ServiceEntry
        {
            Id = Guid.CreateVersion7(),
            WorkOrderId = workOrder.Id,
            TechnicianId = technicianId,
            Note = note,
            IsCorrection = true,
            PhotoUrls = [.. photoUrls],
            CreatedAt = now,
        };

        var entriesOfWorkOrder = previousEntries.Where(previous => previous.WorkOrderId == workOrder.Id).ToList();
        foreach (var usage in returnedParts)
        {
            var consumption = ConsumptionOf(usage.Part.Id, entriesOfWorkOrder);
            if (consumption is null || usage.Quantity > consumption.NetQuantity)
            {
                return ServiceEntryErrors.ReturnExceedsConsumption;
            }

            usage.Part.ReturnToStock(usage.Quantity);
            entry._parts.Add(new ServiceEntryPart(usage.Part.Id, usage.Quantity, consumption.LastUnitPrice));
        }

        return entry;
    }

    private static PartConsumption? ConsumptionOf(Guid partId, IReadOnlyCollection<ServiceEntry> entries)
    {
        var lines = entries
            .SelectMany(entry => entry.Parts
                .Where(line => line.PartId == partId)
                .Select(line => (Entry: entry, Line: line)))
            .ToList();
        var usages = lines
            .Where(item => !item.Entry.IsCorrection)
            .OrderBy(item => item.Entry.CreatedAt)
            .ThenBy(item => item.Entry.Id)
            .ToList();
        if (usages.Count == 0)
        {
            return null;
        }

        var returnedQuantity = lines.Where(item => item.Entry.IsCorrection).Sum(item => item.Line.Quantity);

        return new PartConsumption(usages.Sum(item => item.Line.Quantity) - returnedQuantity, usages[^1].Line.UnitPrice);
    }

    private sealed record PartConsumption(int NetQuantity, decimal LastUnitPrice);
}
