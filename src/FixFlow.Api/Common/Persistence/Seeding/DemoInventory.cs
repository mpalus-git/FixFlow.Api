using FixFlow.Api.Domain.Clients;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.Parts;

namespace FixFlow.Api.Common.Persistence.Seeding;

public sealed class DemoInventory
{
    public const string RetiredClientName = "Restauracja Stara Kuźnia";
    public const string RetiredPartCatalogNumber = "FLT-CARB-OLD";

    private static readonly ClientSeed[] ClientSeeds =
    [
        new(
            "Biuro Rachunkowe Bilans Sp. z o.o.",
            new Address("ul. Mickiewicza", "12", "40-092", "Katowice"),
            "Katarzyna Lis",
            "+48 32 555 01 12",
            "biuro@bilans.example",
            [
                new("RIC-IMC3000-7F21A4", "IM C3000", "Ricoh", new DateOnly(2022, 3, 14)),
                new("DAI-FTXM35R-3302117", "FTXM35R", "Daikin", new DateOnly(2021, 6, 2)),
            ]),
        new(
            "Hotel Pod Lipami",
            new Address("ul. Gliwicka", "45", "44-100", "Gliwice"),
            "Marek Zając",
            "+48 32 555 02 45",
            "recepcja@podlipami.example",
            [
                new("MIT-MSZLN25-8812034", "MSZ-LN25VG", "Mitsubishi Electric", new DateOnly(2020, 5, 18)),
                new("MIT-MSZLN35-8812035", "MSZ-LN35VG", "Mitsubishi Electric", new DateOnly(2020, 5, 18)),
                new("LG-ARUM100-4410298", "Multi V 5 ARUM100", "LG", new DateOnly(2019, 9, 30)),
            ]),
        new(
            "Przychodnia Zdrowie Plus",
            new Address("ul. Warszawska", "8", "41-200", "Sosnowiec"),
            "Ewa Kamińska",
            "+48 32 555 03 08",
            "administracja@zdrowieplus.example",
            [
                new("SAM-AR12TXEA-5520411", "Wind-Free AR12TXEA", "Samsung", new DateOnly(2023, 4, 11)),
                new("KYO-TA2554CI-0093312", "TASKalfa 2554ci", "Kyocera", new DateOnly(2022, 11, 7)),
                new("CAN-IR2425-2271840", "imageRUNNER 2425", "Canon", new DateOnly(2021, 1, 25)),
            ]),
        new(
            "Drukarnia Kolorowa s.c.",
            new Address("ul. Przemysłowa", "3", "43-300", "Bielsko-Biała"),
            "Paweł Mazur",
            "+48 33 555 04 03",
            "zamowienia@kolorowa.example",
            [
                new("KM-C450I-6610032", "bizhub C450i", "Konica Minolta", new DateOnly(2023, 8, 21)),
                new("TOS-ES5018A-1178214", "e-STUDIO 5018A", "Toshiba", new DateOnly(2020, 2, 3)),
                new("DAI-FTXF50D-3310552", "FTXF50D", "Daikin", new DateOnly(2022, 7, 15)),
            ]),
        new(
            "Kancelaria Notarialna Nowicka",
            new Address("ul. Długa", "21/4", "31-147", "Kraków"),
            "Joanna Nowicka",
            "+48 12 555 05 21",
            "kancelaria@nowicka.example",
            [
                new("HP-M528DN-CNB2K4101", "LaserJet Enterprise MFP M528dn", "HP", new DateOnly(2024, 1, 9)),
            ]),
        new(
            RetiredClientName,
            new Address("Rynek", "5", "44-200", "Rybnik"),
            "Grzegorz Wróbel",
            "+48 32 555 06 05",
            "kontakt@starakuznia.example",
            [
                new("GRE-GWH12AGB-5003921", "Pular GWH12AGB", "Gree", new DateOnly(2018, 6, 12)),
            ]),
    ];

    private static readonly PartSeed[] PartSeeds =
    [
        new("Filtr powietrza do klimatyzatora ściennego", "FLT-AC-100", 40, 45.00m),
        new("Czynnik chłodniczy R32 (1 kg)", "REF-R32-1KG", 25, 89.90m),
        new("Kondensator rozruchowy 35 µF", "CAP-35UF", 15, 38.50m),
        new("Pompka skroplin", "PMP-CND-01", 1, 210.00m),
        new("Czujnik temperatury NTC 10 kΩ", "SNS-NTC-10K", 20, 24.90m),
        new("Toner czarny do Ricoh IM C3000", "TNR-RIC-C3000-K", 8, 219.00m),
        new("Bęben światłoczuły", "DRM-UNI-01", 5, 480.00m),
        new("Zespół utrwalający", "FSR-UNI-220", 3, 890.00m),
        new("Rolka podająca papier", "RLR-FEED-01", 30, 32.00m),
        new("Pas transferowy", "BLT-TRF-01", 2, 640.00m),
        new("Filtr węglowy (wycofany)", RetiredPartCatalogNumber, 4, 29.00m),
    ];

    private DemoInventory(IReadOnlyList<Client> clients, IReadOnlyList<Device> devices, IReadOnlyList<Part> parts)
    {
        Clients = clients;
        Devices = devices;
        Parts = parts;
    }

    public IReadOnlyList<Client> Clients { get; }

    public IReadOnlyList<Device> Devices { get; }

    public IReadOnlyList<Part> Parts { get; }

    public static DemoInventory Create(DateTimeOffset createdAt)
    {
        var clients = new List<Client>();
        var devices = new List<Device>();
        foreach (var clientSeed in ClientSeeds)
        {
            var client = Client.Create(
                clientSeed.Name,
                clientSeed.Address,
                clientSeed.ContactPerson,
                clientSeed.Phone,
                clientSeed.Email,
                createdAt);
            clients.Add(client);
            devices.AddRange(clientSeed.Devices.Select(deviceSeed => Device.Create(
                client,
                deviceSeed.SerialNumber,
                deviceSeed.Model,
                deviceSeed.Manufacturer,
                deviceSeed.InstallationDate,
                createdAt).Value));
        }

        var parts = PartSeeds
            .Select(partSeed => Part.Create(partSeed.Name, partSeed.CatalogNumber, partSeed.StockQuantity, partSeed.UnitPrice, createdAt))
            .ToList();

        return new DemoInventory(clients, devices, parts);
    }

    public Device DeviceWithSerialNumber(string serialNumber) =>
        Devices.Single(device => device.SerialNumber == serialNumber);

    public Part PartWithCatalogNumber(string catalogNumber) =>
        Parts.Single(part => part.CatalogNumber == catalogNumber);

    public void ArchiveRetiredItems(DateTimeOffset archivedAt)
    {
        var retiredClient = Clients.Single(client => client.Name == RetiredClientName);
        retiredClient.Archive(archivedAt);
        foreach (var device in Devices.Where(device => device.ClientId == retiredClient.Id))
        {
            device.Archive(archivedAt);
        }

        PartWithCatalogNumber(RetiredPartCatalogNumber).Archive(archivedAt);
    }

    private sealed record ClientSeed(
        string Name,
        Address Address,
        string ContactPerson,
        string Phone,
        string Email,
        DeviceSeed[] Devices);

    private sealed record DeviceSeed(string SerialNumber, string Model, string Manufacturer, DateOnly InstallationDate);

    private sealed record PartSeed(string Name, string CatalogNumber, int StockQuantity, decimal UnitPrice);
}
