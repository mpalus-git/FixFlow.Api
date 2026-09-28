using ErrorOr;

namespace FixFlow.Api.Domain.Parts;

public sealed class Part
{
    private Part()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string CatalogNumber { get; private set; } = string.Empty;

    public int StockQuantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public bool IsArchived => ArchivedAt is not null;

    public static Part Create(string name, string catalogNumber, int stockQuantity, decimal unitPrice, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name,
        CatalogNumber = NormalizeCatalogNumber(catalogNumber),
        StockQuantity = stockQuantity,
        UnitPrice = unitPrice,
        CreatedAt = now,
    };

    public static string NormalizeCatalogNumber(string catalogNumber) => catalogNumber.Trim().ToUpperInvariant();

    public ErrorOr<Updated> Update(string name, string catalogNumber, decimal unitPrice)
    {
        if (IsArchived)
        {
            return PartErrors.Archived;
        }

        Name = name;
        CatalogNumber = NormalizeCatalogNumber(catalogNumber);
        UnitPrice = unitPrice;

        return Result.Updated;
    }

    public void Restock(int quantity)
    {
        StockQuantity += quantity;
    }

    public ErrorOr<Success> EnsureCanConsume(int quantity)
    {
        if (IsArchived)
        {
            return PartErrors.Archived;
        }

        if (quantity > StockQuantity)
        {
            return PartErrors.InsufficientStock;
        }

        return Result.Success;
    }

    public ErrorOr<Updated> Consume(int quantity)
    {
        var check = EnsureCanConsume(quantity);
        if (check.IsError)
        {
            return check.Errors;
        }

        StockQuantity -= quantity;

        return Result.Updated;
    }

    public void ReturnToStock(int quantity)
    {
        StockQuantity += quantity;
    }

    public void Archive(DateTimeOffset now)
    {
        ArchivedAt ??= now;
    }
}
