namespace FixFlow.Api.Common.Concurrency;

public sealed record Versioned<TValue>(TValue Value, uint Version);
