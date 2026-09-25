namespace FixFlow.Api.Domain.ServiceEntries;

public sealed record UsedPart(Guid PartId, int Quantity, decimal UnitPrice)
{
    public decimal Value => Quantity * UnitPrice;
}
