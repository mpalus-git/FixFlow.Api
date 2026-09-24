using System.ComponentModel;

namespace FixFlow.Api.Features.Parts.UpdatePart;

[Description("New data of an existing part. All fields are replaced; the stock quantity changes only through deliveries and service entries.")]
public sealed record UpdatePartRequest(
    [property: Description("Part name.")] string Name,
    [property: Description("Catalog number, unique across all parts regardless of letter case and surrounding spaces.")] string CatalogNumber,
    [property: Description("Net unit price in PLN with at most two decimal places.")] decimal UnitPrice) : IPartDetails;
