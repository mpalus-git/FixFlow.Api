using FixFlow.Api.Domain.Clients;
using FixFlow.Api.Domain.Devices;

namespace FixFlow.Api.UnitTests.Domain.Devices;

public sealed class DeviceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly InstallationDate = new(2024, 5, 20);

    [Fact]
    public void Should_Create_Active_Device_Assigned_To_Client_When_Client_Is_Active()
    {
        var client = CreateClient();

        var result = Device.Create(client, "AC-1001", "Split 3.5 kW", "Daikin", InstallationDate, Now);

        result.IsError.ShouldBeFalse();
        var device = result.Value;
        device.Id.ShouldNotBe(Guid.Empty);
        device.ClientId.ShouldBe(client.Id);
        device.Model.ShouldBe("Split 3.5 kW");
        device.Manufacturer.ShouldBe("Daikin");
        device.InstallationDate.ShouldBe(InstallationDate);
        device.CreatedAt.ShouldBe(Now);
        device.IsArchived.ShouldBeFalse();
    }

    [Fact]
    public void Should_Store_Trimmed_Upper_Case_Serial_Number_When_Created()
    {
        var result = Device.Create(CreateClient(), "  ac-1001x ", "Split 3.5 kW", "Daikin", InstallationDate, Now);

        result.Value.SerialNumber.ShouldBe("AC-1001X");
    }

    [Fact]
    public void Should_Reject_Creating_Device_When_Client_Is_Archived()
    {
        var client = CreateClient();
        client.Archive(Now);

        var result = Device.Create(client, "AC-1001", "Split 3.5 kW", "Daikin", InstallationDate, Now);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(DeviceErrors.ClientArchived);
    }

    [Fact]
    public void Should_Change_Details_And_Normalize_Serial_Number_When_Active_Device_Is_Updated()
    {
        var device = CreateDevice();

        var result = device.Update(" ac-2002 ", "Multi 5 kW", "Mitsubishi", InstallationDate.AddDays(1));

        result.IsError.ShouldBeFalse();
        device.SerialNumber.ShouldBe("AC-2002");
        device.Model.ShouldBe("Multi 5 kW");
        device.Manufacturer.ShouldBe("Mitsubishi");
        device.InstallationDate.ShouldBe(InstallationDate.AddDays(1));
    }

    [Fact]
    public void Should_Reject_Update_When_Device_Is_Archived()
    {
        var device = CreateDevice();
        device.Archive(Now.AddDays(1));

        var result = device.Update("AC-2002", "Multi 5 kW", "Mitsubishi", InstallationDate);

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(DeviceErrors.Archived);
        device.SerialNumber.ShouldBe("AC-1001");
    }

    [Fact]
    public void Should_Keep_First_Archive_Time_When_Archived_Twice()
    {
        var device = CreateDevice();

        device.Archive(Now.AddDays(1));
        device.Archive(Now.AddDays(2));

        device.IsArchived.ShouldBeTrue();
        device.ArchivedAt.ShouldBe(Now.AddDays(1));
    }

    private static Client CreateClient() =>
        Client.Create("Klimat-Serwis", new Address("Marszałkowska", "10A", "00-590", "Warszawa"), "Anna Nowak", "+48 600 100 200", null, Now);

    private static Device CreateDevice() =>
        Device.Create(CreateClient(), "AC-1001", "Split 3.5 kW", "Daikin", InstallationDate, Now).Value;
}
