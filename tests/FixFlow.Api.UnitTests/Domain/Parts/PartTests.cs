using FixFlow.Api.Domain.Parts;

namespace FixFlow.Api.UnitTests.Domain.Parts;

public sealed class PartTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Should_Create_Active_Part_With_Initial_Stock_When_Created()
    {
        var part = Part.Create("Filtr powietrza", "FLT-100", 12, 49.99m, Now);

        part.Id.ShouldNotBe(Guid.Empty);
        part.Name.ShouldBe("Filtr powietrza");
        part.StockQuantity.ShouldBe(12);
        part.UnitPrice.ShouldBe(49.99m);
        part.CreatedAt.ShouldBe(Now);
        part.IsArchived.ShouldBeFalse();
    }

    [Fact]
    public void Should_Store_Trimmed_Upper_Case_Catalog_Number_When_Created()
    {
        var part = Part.Create("Filtr powietrza", "  flt-100x ", 0, 49.99m, Now);

        part.CatalogNumber.ShouldBe("FLT-100X");
    }

    [Fact]
    public void Should_Change_Details_Without_Changing_Stock_When_Active_Part_Is_Updated()
    {
        var part = CreatePart();

        var result = part.Update("Filtr węglowy", " flt-200 ", 59.50m);

        result.IsError.ShouldBeFalse();
        part.Name.ShouldBe("Filtr węglowy");
        part.CatalogNumber.ShouldBe("FLT-200");
        part.UnitPrice.ShouldBe(59.50m);
        part.StockQuantity.ShouldBe(12);
    }

    [Fact]
    public void Should_Reject_Update_When_Part_Is_Archived()
    {
        var part = CreatePart();
        part.Archive(Now.AddDays(1));

        var result = part.Update("Filtr węglowy", "FLT-200", 59.50m);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(PartErrors.Archived);
        part.CatalogNumber.ShouldBe("FLT-100");
    }

    [Fact]
    public void Should_Increase_Stock_When_Part_Is_Restocked()
    {
        var part = CreatePart();

        part.Restock(8);

        part.StockQuantity.ShouldBe(20);
    }

    [Fact]
    public void Should_Increase_Stock_When_Archived_Part_Is_Restocked()
    {
        var part = CreatePart();
        part.Archive(Now.AddDays(1));

        part.Restock(3);

        part.StockQuantity.ShouldBe(15);
    }

    [Theory]
    [InlineData(1, 11)]
    [InlineData(12, 0)]
    public void Should_Decrease_Stock_When_Consumed_Quantity_Does_Not_Exceed_Stock(int quantity, int expectedStock)
    {
        var part = CreatePart();

        var result = part.Consume(quantity);

        result.IsError.ShouldBeFalse();
        part.StockQuantity.ShouldBe(expectedStock);
    }

    [Fact]
    public void Should_Reject_Consumption_And_Keep_Stock_When_It_Would_Drop_Stock_Below_Zero()
    {
        var part = CreatePart();

        var result = part.Consume(13);

        result.FirstError.ShouldBe(PartErrors.InsufficientStock);
        part.StockQuantity.ShouldBe(12);
    }

    [Fact]
    public void Should_Allow_Consumption_Without_Changing_Stock_When_Checking_Quantity_Within_Stock()
    {
        var part = CreatePart();

        var result = part.EnsureCanConsume(12);

        result.IsError.ShouldBeFalse();
        part.StockQuantity.ShouldBe(12);
    }

    [Fact]
    public void Should_Reject_Consumption_When_Part_Is_Archived()
    {
        var part = CreatePart();
        part.Archive(Now.AddDays(1));

        var result = part.Consume(1);

        result.FirstError.ShouldBe(PartErrors.Archived);
        part.StockQuantity.ShouldBe(12);
    }

    [Fact]
    public void Should_Increase_Stock_When_Archived_Part_Is_Returned_To_Stock()
    {
        var part = CreatePart();
        part.Archive(Now.AddDays(1));

        part.ReturnToStock(2);

        part.StockQuantity.ShouldBe(14);
    }

    [Fact]
    public void Should_Keep_First_Archive_Time_When_Archived_Twice()
    {
        var part = CreatePart();

        part.Archive(Now.AddDays(1));
        part.Archive(Now.AddDays(2));

        part.IsArchived.ShouldBeTrue();
        part.ArchivedAt.ShouldBe(Now.AddDays(1));
    }

    private static Part CreatePart() => Part.Create("Filtr powietrza", "FLT-100", 12, 49.99m, Now);
}
