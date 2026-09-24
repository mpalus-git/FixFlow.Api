namespace FixFlow.Api.Domain.ServiceEntries;

public sealed class ServiceEntryPart
{
    public ServiceEntryPart(Guid partId, int quantity, decimal unitPrice)
    {
        PartId = partId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid PartId { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }
}
