using System.ComponentModel;

namespace FixFlow.Api.Features.Parts.CreatePart;

[Description("Data of a new part.")]
public sealed record CreatePartRequest(
    [property: Description("Part name.")] string Name,
    [property: Description("Catalog number, unique across all parts regardless of letter case and surrounding spaces.")] string CatalogNumber,
    [property: Description("Initial number of units in stock, from 0 to 100000.")] int StockQuantity,
    [property: Description("Net unit price in PLN with at most two decimal places.")] decimal UnitPrice) : IPartDetails;
