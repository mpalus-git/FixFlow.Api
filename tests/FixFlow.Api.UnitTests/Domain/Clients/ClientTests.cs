using FixFlow.Api.Domain.Clients;

namespace FixFlow.Api.UnitTests.Domain.Clients;

public sealed class ClientTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly Address OfficeAddress = new("Marszałkowska", "10A", "00-590", "Warszawa");
    private static readonly Address WarehouseAddress = new("Przemysłowa", "3/1", "30-701", "Kraków");

    [Fact]
    public void Should_Create_Active_Client_With_Given_Details_When_Created()
    {
        var client = Client.Create("Klimat-Serwis", OfficeAddress, "Anna Nowak", "+48 600 100 200", "biuro@klimat.test", Now);

        client.Id.ShouldNotBe(Guid.Empty);
        client.Name.ShouldBe("Klimat-Serwis");
        client.Address.ShouldBe(OfficeAddress);
        client.CreatedAt.ShouldBe(Now);
        client.IsArchived.ShouldBeFalse();
    }

    [Fact]
    public void Should_Change_Details_When_Active_Client_Is_Updated()
    {
        var client = Client.Create("Klimat-Serwis", OfficeAddress, "Anna Nowak", "+48 600 100 200", null, Now);

        var result = client.Update("Klimat-Serwis Sp. z o.o.", WarehouseAddress, "Jan Kowalski", "+48 600 300 400", "kontakt@klimat.test");

        result.IsError.ShouldBeFalse();
        client.Name.ShouldBe("Klimat-Serwis Sp. z o.o.");
        client.Address.ShouldBe(WarehouseAddress);
        client.ContactPerson.ShouldBe("Jan Kowalski");
        client.Phone.ShouldBe("+48 600 300 400");
        client.Email.ShouldBe("kontakt@klimat.test");
    }

    [Fact]
    public void Should_Reject_Update_When_Client_Is_Archived()
    {
        var client = Client.Create("Klimat-Serwis", OfficeAddress, "Anna Nowak", "+48 600 100 200", null, Now);
        client.Archive(Now.AddDays(1));

        var result = client.Update("New name", WarehouseAddress, "Jan Kowalski", "+48 600 300 400", null);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(ClientErrors.Archived);
        client.Name.ShouldBe("Klimat-Serwis");
    }

    [Fact]
    public void Should_Keep_First_Archive_Time_When_Archived_Twice()
    {
        var client = Client.Create("Klimat-Serwis", OfficeAddress, "Anna Nowak", "+48 600 100 200", null, Now);

        client.Archive(Now.AddDays(1));
        client.Archive(Now.AddDays(2));

        client.IsArchived.ShouldBeTrue();
        client.ArchivedAt.ShouldBe(Now.AddDays(1));
    }
}
