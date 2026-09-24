using System.ComponentModel;
using FixFlow.Api.Domain.Parts;

namespace FixFlow.Api.Features.Parts;

[Description("Spare part from the warehouse catalog.")]
public sealed record PartResponse(
    [property: Description("Identifier of the part.")] Guid Id,
    [property: Description("Part name.")] string Name,
    [property: Description("Unique catalog number, stored trimmed and in upper case.")] string CatalogNumber,
    [property: Description("Number of units currently in stock.")] int StockQuantity,
    [property: Description("Current net unit price in PLN.")] decimal UnitPrice,
    [property: Description("UTC time when the part was created.")] DateTimeOffset CreatedAt,
    [property: Description("UTC time when the part was archived; null for active parts.")] DateTimeOffset? ArchivedAt)
{
    public static PartResponse FromDomain(Part part) => new(
        part.Id,
        part.Name,
        part.CatalogNumber,
        part.StockQuantity,
        part.UnitPrice,
        part.CreatedAt,
        part.ArchivedAt);
}
