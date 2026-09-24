using System.ComponentModel;

namespace FixFlow.Api.Features.ServiceEntries.AddServiceEntry;

[Description("New service entry of a work order in progress. A work entry records performed work and used parts; a correction entry returns wrongly recorded parts to stock and has no work time or location.")]
public sealed record AddServiceEntryRequest(
    [property: Description("Description of the performed work or, for a correction, its reason.")] string Note,
    [property: Description("True for a correction entry that returns parts to stock; false for a work entry.")] bool IsCorrection = false,
    [property: Description("Absolute http or https addresses of photos, at most 10.")] IReadOnlyList<string>? PhotoUrls = null,
    [property: Description("Time when the work started; required for a work entry and not allowed for a correction. Cannot be earlier than the start of the work order.")] DateTimeOffset? WorkStartedAt = null,
    [property: Description("Time when the work finished; required for a work entry and not allowed for a correction. Must be after the start and not in the future.")] DateTimeOffset? WorkFinishedAt = null,
    [property: Description("Latitude of the place where the work started, from -90 to 90; optional, given together with longitude and not allowed for a correction.")] double? Latitude = null,
    [property: Description("Longitude of the place where the work started, from -180 to 180; optional, given together with latitude and not allowed for a correction.")] double? Longitude = null,
    [property: Description("Parts used in a work entry or returned in a correction, at most 20, each part at most once. Required for a correction.")] IReadOnlyList<ServiceEntryPartRequest>? Parts = null);

[Description("Quantity of one part used or returned in a service entry.")]
public sealed record ServiceEntryPartRequest(
    [property: Description("Identifier of the part.")] Guid PartId,
    [property: Description("Number of units, from 1 to 1000.")] int Quantity);
