using System.ComponentModel;
using FixFlow.Api.Domain.Parts;
using FixFlow.Api.Domain.ServiceEntries;

namespace FixFlow.Api.Features.ServiceEntries;

[Description("Service entry of a work order: performed work with used parts, or a correction returning parts to stock.")]
public sealed record ServiceEntryResponse(
    [property: Description("Identifier of the service entry.")] Guid Id,
    [property: Description("Identifier of the work order.")] Guid WorkOrderId,
    [property: Description("Identifier of the technician who added the entry.")] Guid TechnicianId,
    [property: Description("First and last name of the technician who added the entry.")] string TechnicianName,
    [property: Description("Description of the performed work or the reason of the correction.")] string Note,
    [property: Description("True for a correction entry whose parts were returned to stock.")] bool IsCorrection,
    [property: Description("Addresses of attached photos.")] IReadOnlyList<string> PhotoUrls,
    [property: Description("UTC time when the work started; null for a correction.")] DateTimeOffset? WorkStartedAt,
    [property: Description("UTC time when the work finished; null for a correction.")] DateTimeOffset? WorkFinishedAt,
    [property: Description("Latitude of the place where the work started, if recorded.")] double? Latitude,
    [property: Description("Longitude of the place where the work started, if recorded.")] double? Longitude,
    [property: Description("Parts used in a work entry or returned in a correction.")] IReadOnlyList<ServiceEntryPartResponse> Parts,
    [property: Description("UTC time when the entry was added.")] DateTimeOffset CreatedAt)
{
    public static ServiceEntryResponse FromDomain(ServiceEntry entry, string technicianName, IReadOnlyDictionary<Guid, Part> parts) => new(
        entry.Id,
        entry.WorkOrderId,
        entry.TechnicianId,
        technicianName,
        entry.Note,
        entry.IsCorrection,
        entry.PhotoUrls,
        entry.WorkStartedAt,
        entry.WorkFinishedAt,
        entry.Latitude,
        entry.Longitude,
        [.. entry.Parts.Select(part => new ServiceEntryPartResponse(part.PartId, parts[part.PartId].Name, parts[part.PartId].CatalogNumber, part.Quantity, part.UnitPrice))],
        entry.CreatedAt);
}

[Description("Quantity of one part in a service entry with its catalog details and unit price at the time of use.")]
public sealed record ServiceEntryPartResponse(
    [property: Description("Identifier of the part.")] Guid PartId,
    [property: Description("Current name of the part in the catalog, also for archived parts.")] string PartName,
    [property: Description("Current catalog number of the part, also for archived parts.")] string CatalogNumber,
    [property: Description("Number of used or returned units.")] int Quantity,
    [property: Description("Net unit price in PLN at the time the part was used; for a correction, the price of the last use on the work order.")] decimal UnitPrice);
