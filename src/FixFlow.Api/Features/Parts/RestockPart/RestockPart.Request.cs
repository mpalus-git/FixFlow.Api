using System.ComponentModel;

namespace FixFlow.Api.Features.Parts.RestockPart;

[Description("Delivery of units of a part to the warehouse.")]
public sealed record RestockPartRequest(
    [property: Description("Number of delivered units, from 1 to 100000.")] int Quantity);
