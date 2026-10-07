using FixFlow.Api.Domain.Clients;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.ServiceEntries;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Features.WorkOrders.GetServiceProtocol;

public sealed record ServiceProtocol(
    WorkOrder WorkOrder,
    Device Device,
    Client Client,
    string? TechnicianName,
    IReadOnlyList<ServiceEntry> ServiceEntries,
    IReadOnlyList<ProtocolPartLine> PartLines,
    byte[]? ClientSignature,
    DateTimeOffset IssuedAt)
{
    public decimal PartsTotal => PartLines.Sum(line => line.Value);

    public TimeSpan TotalWorkTime => ServiceEntries
        .Where(entry => entry.WorkStartedAt is not null && entry.WorkFinishedAt is not null)
        .Aggregate(TimeSpan.Zero, (total, entry) => total + (entry.WorkFinishedAt!.Value - entry.WorkStartedAt!.Value));
}

public sealed record ServiceProtocolFile(string FileName, byte[] Content)
{
    public static ServiceProtocolFile For(WorkOrder workOrder, byte[] content) =>
        new($"protokol-{workOrder.Number.Replace('/', '-')}.pdf", content);
}

public sealed record ProtocolPartLine(string Name, string CatalogNumber, int Quantity, decimal UnitPrice)
{
    public decimal Value => Quantity * UnitPrice;
}
