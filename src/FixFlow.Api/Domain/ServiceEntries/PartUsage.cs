using FixFlow.Api.Domain.Parts;

namespace FixFlow.Api.Domain.ServiceEntries;

public sealed record PartUsage(Part Part, int Quantity);
