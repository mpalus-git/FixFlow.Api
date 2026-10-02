using ErrorOr;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.ServiceEntries;
using FixFlow.Api.Domain.WorkOrders;

namespace FixFlow.Api.Common.Persistence.Seeding;

public sealed class DemoWorkOrderHistory
{
    private const string WallAirConditionerPhotoUrl =
        "https://upload.wikimedia.org/wikipedia/commons/thumb/b/b8/Air_conditioner_ballu.jpg/960px-Air_conditioner_ballu.jpg";
    private const string AgedWallAirConditionerPhotoUrl =
        "https://upload.wikimedia.org/wikipedia/commons/thumb/1/16/HYUNDAI_-_Air_conditioner_mini_split_%28model_BMS-12HD%29.jpg/960px-HYUNDAI_-_Air_conditioner_mini_split_%28model_BMS-12HD%29.jpg";
    private const string VrfOutdoorUnitsPhotoUrl =
        "https://upload.wikimedia.org/wikipedia/commons/thumb/0/06/Daikin_Ac_Repair_Maintenance_at_godawori_nepal.jpg/960px-Daikin_Ac_Repair_Maintenance_at_godawori_nepal.jpg";
    private const string ProductionCopierPhotoUrl =
        "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9c/The_Closest_Photocopier_%283594209596%29.jpg/960px-The_Closest_Photocopier_%283594209596%29.jpg";
    private const string TonerCartridgePhotoUrl =
        "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9e/Tonerkassette_Laserdrucker_HP.jpg/960px-Tonerkassette_Laserdrucker_HP.jpg";

    private static readonly TimeSpan EntryRecordingDelay = TimeSpan.FromMinutes(10);

    private static readonly Dictionary<string, GpsLocation> LocationsByCity = new()
    {
        ["Katowice"] = new GpsLocation(50.2598, 19.0215),
        ["Gliwice"] = new GpsLocation(50.2945, 18.6714),
        ["Sosnowiec"] = new GpsLocation(50.2863, 19.1041),
        ["Bielsko-Biała"] = new GpsLocation(49.8224, 19.0444),
        ["Kraków"] = new GpsLocation(50.0614, 19.9366),
        ["Rybnik"] = new GpsLocation(50.0971, 18.5463),
    };

    private readonly DemoInventory _inventory;
    private readonly DateTimeOffset _now;
    private readonly List<WorkOrder> _workOrders = [];
    private readonly List<ServiceEntry> _serviceEntries = [];

    private DemoWorkOrderHistory(DemoInventory inventory, DateTimeOffset now)
    {
        _inventory = inventory;
        _now = now;
    }

    public IReadOnlyList<WorkOrder> WorkOrders => _workOrders;

    public IReadOnlyList<ServiceEntry> ServiceEntries => _serviceEntries;

    public static DemoWorkOrderHistory Create(DemoInventory inventory, DemoTechnicians technicians, DateTimeOffset now)
    {
        var history = new DemoWorkOrderHistory(inventory, now);
        history.AddLoginTechnicianWorkOrders(technicians.LoginTechnicianId);
        history.AddAnnaKowalczykWorkOrders(technicians.AnnaKowalczykId);
        history.AddTomaszWojcikWorkOrders(technicians.TomaszWojcikId);
        history.AddPiotrZielinskiWorkOrders(technicians.PiotrZielinskiId);
        history.AddUnassignedWorkOrders();
        return history;
    }

    private void AddLoginTechnicianWorkOrders(Guid technicianId)
    {
        Open("RIC-IMC3000-7F21A4", "Zacięcia papieru w podajniku nr 2, komunikat J-012.", WorkOrderPriority.Normal, createdDaysAgo: 40, dueDaysAfterCreation: 5)
            .AssignTo(technicianId).Start(daysAgo: 38.9)
            .Work("Wymieniono rolki podające podajnika nr 2, wyczyszczono tor papieru. Wykonano 50 wydruków testowych bez zacięć.", startedDaysAgo: 38.9, hours: 1.5, ("RLR-FEED-01", 2))
            .Complete(daysAgo: 38.8).Invoice(daysAgo: 35);
        Open("DAI-FTXM35R-3302117", "Klimatyzator w sali konferencyjnej nie chłodzi, podejrzenie ubytku czynnika.", WorkOrderPriority.High, createdDaysAgo: 6, dueDaysAfterCreation: 4)
            .AssignTo(technicianId).Start(daysAgo: 4.2)
            .Work("Zlokalizowano i uszczelniono nieszczelność na kielichu. Próba ciśnieniowa, próżnia, uzupełniono czynnik R32.", startedDaysAgo: 4.2, hours: 2.5, [WallAirConditionerPhotoUrl], ("REF-R32-1KG", 2))
            .Complete(daysAgo: 4.05);
        Open("MIT-MSZLN35-8812035", "Wyciek wody z jednostki wewnętrznej w pokoju 204.", WorkOrderPriority.High, createdDaysAgo: 1.5, dueDaysAfterCreation: 2)
            .AssignTo(technicianId).Start(daysAgo: 0.1)
            .Work("Diagnoza: niedrożny odpływ skroplin i uszkodzona pompka. Udrożniono odpływ, pompka do wymiany.", startedDaysAgo: 0.1, hours: 1, [AgedWallAirConditionerPhotoUrl]);
        Open("KYO-TA2554CI-0093312", "Smugi na wydrukach kolorowych, głównie w kolorze magenta.", WorkOrderPriority.Normal, createdDaysAgo: 5, dueDaysAfterCreation: 3)
            .AssignTo(technicianId);
        Open("CAN-IR2425-2271840", "Wymiana bębna po przekroczeniu licznika eksploatacji.", WorkOrderPriority.Low, createdDaysAgo: 1, dueDaysAfterCreation: 6)
            .AssignTo(technicianId);
        Open("KM-C450I-6610032", "Błąd zespołu utrwalającego C2557, urządzenie zablokowane.", WorkOrderPriority.High, createdDaysAgo: 25, dueDaysAfterCreation: 2)
            .AssignTo(technicianId).Start(daysAgo: 24)
            .Work("Wymieniono zespół utrwalający i rolki podające. Skasowano błąd, wykonano kalibrację.", startedDaysAgo: 24, hours: 3, [ProductionCopierPhotoUrl], ("FSR-UNI-220", 1), ("RLR-FEED-01", 2))
            .Correction("Korekta: jedna rolka podająca nie została zamontowana, zwrócono ją na magazyn.", daysAgo: 23.8, ("RLR-FEED-01", 1))
            .Complete(daysAgo: 23.75).Invoice(daysAgo: 20);
    }

    private void AddAnnaKowalczykWorkOrders(Guid technicianId)
    {
        Open("LG-ARUM100-4410298", "Przegląd okresowy systemu VRF przed sezonem letnim.", WorkOrderPriority.Low, createdDaysAgo: 60, dueDaysAfterCreation: 14)
            .AssignTo(technicianId).Start(daysAgo: 50)
            .Work("Przegląd jednostki zewnętrznej i czterech jednostek wewnętrznych. Wymieniono filtry, wymieniono uszkodzony czujnik temperatury.", startedDaysAgo: 50, hours: 4, [VrfOutdoorUnitsPhotoUrl, WallAirConditionerPhotoUrl], ("FLT-AC-100", 4), ("SNS-NTC-10K", 1))
            .Complete(daysAgo: 49.8).Invoice(daysAgo: 45);
        Open("SAM-AR12TXEA-5520411", "Głośna praca wentylatora jednostki wewnętrznej w gabinecie nr 3.", WorkOrderPriority.Normal, createdDaysAgo: 10, dueDaysAfterCreation: 5)
            .AssignTo(technicianId).Start(daysAgo: 8)
            .Work("Wyczyszczono i wyważono wentylator, dokręcono mocowania obudowy. Praca cicha.", startedDaysAgo: 8, hours: 1.5)
            .Complete(daysAgo: 7.9);
        Open("DAI-FTXF50D-3310552", "Klimatyzator w serwerowni wyłącza się z błędem U4.", WorkOrderPriority.Critical, createdDaysAgo: 0.5, dueDaysAfterCreation: 1)
            .AssignTo(technicianId).Start(daysAgo: 0.2);
        Open("GRE-GWH12AGB-5003921", "Przegląd klimatyzatora na sali i wymiana filtra węglowego.", WorkOrderPriority.Low, createdDaysAgo: 35, dueDaysAfterCreation: 10)
            .AssignTo(technicianId).Start(daysAgo: 33)
            .Work("Wykonano przegląd, odgrzybianie parownika i wymianę filtra węglowego.", startedDaysAgo: 33, hours: 2, ("FLT-CARB-OLD", 1))
            .Complete(daysAgo: 32.9).Invoice(daysAgo: 30);
    }

    private void AddTomaszWojcikWorkOrders(Guid technicianId)
    {
        Open("TOS-ES5018A-1178214", "Wymiana pasa transferowego, pasy na wydrukach.", WorkOrderPriority.Normal, createdDaysAgo: 45, dueDaysAfterCreation: 7)
            .AssignTo(technicianId).Start(daysAgo: 44)
            .Work("Wymieniono pas transferowy, wyczyszczono korony ładujące. Jakość wydruku prawidłowa.", startedDaysAgo: 44, hours: 2, ("BLT-TRF-01", 1))
            .Complete(daysAgo: 43.9).Invoice(daysAgo: 40);
        Open("HP-M528DN-CNB2K4101", "Urządzenie nie wysyła skanów na e-mail.", WorkOrderPriority.Normal, createdDaysAgo: 3, dueDaysAfterCreation: 4)
            .AssignTo(technicianId).Start(daysAgo: 2)
            .Work("Zaktualizowano ustawienia serwera SMTP i certyfikat. Skanowanie na e-mail działa.", startedDaysAgo: 2, hours: 1)
            .Complete(daysAgo: 1.95);
        Open("MIT-MSZLN25-8812034", "Brak grzania w pokoju 112.", WorkOrderPriority.High, createdDaysAgo: 2, dueDaysAfterCreation: 1)
            .AssignTo(technicianId).Start(daysAgo: 1.2)
            .Work("Wymieniono czujnik temperatury parownika. Obserwacja pracy w trybie grzania w toku.", startedDaysAgo: 1.1, hours: 1.5, ("SNS-NTC-10K", 1));
        Open("TOS-ES5018A-1178214", "Zacięcia papieru w finiszerze przy zszywaniu.", WorkOrderPriority.Normal, createdDaysAgo: 0.8, dueDaysAfterCreation: 5)
            .AssignTo(technicianId);
    }

    private void AddPiotrZielinskiWorkOrders(Guid technicianId)
    {
        Open("MIT-MSZLN25-8812034", "Coroczny przegląd klimatyzatorów w pokojach hotelowych.", WorkOrderPriority.Low, createdDaysAgo: 85, dueDaysAfterCreation: 14)
            .AssignTo(technicianId).Start(daysAgo: 80)
            .Work("Przegląd dwóch jednostek wewnętrznych, wymieniono filtry, wyczyszczono tace skroplin.", startedDaysAgo: 80, hours: 3, ("FLT-AC-100", 2))
            .Complete(daysAgo: 79.9).Invoice(daysAgo: 75);
        Open("CAN-IR2425-2271840", "Urządzenie nie pobiera papieru z kasety nr 1.", WorkOrderPriority.Normal, createdDaysAgo: 78, dueDaysAfterCreation: 5)
            .AssignTo(technicianId).Start(daysAgo: 76)
            .Work("Wymieniono zużytą rolkę podającą kasety nr 1, wyczyszczono czujnik obecności papieru.", startedDaysAgo: 76, hours: 1, ("RLR-FEED-01", 1))
            .Complete(daysAgo: 75.95).Invoice(daysAgo: 72);
        Open("DAI-FTXF50D-3310552", "Skropliny kapią z jednostki wewnętrznej w serwerowni.", WorkOrderPriority.High, createdDaysAgo: 72, dueDaysAfterCreation: 2)
            .AssignTo(technicianId).Start(daysAgo: 71.5)
            .Work("Wymieniono uszkodzoną pompkę skroplin, sprawdzono drożność odpływu i szczelność połączeń.", startedDaysAgo: 71.5, hours: 2, ("PMP-CND-01", 1))
            .Complete(daysAgo: 71.4).Invoice(daysAgo: 68);
        Open("RIC-IMC3000-7F21A4", "Wymiana tonera czarnego i blade wydruki w trybie monochromatycznym.", WorkOrderPriority.Low, createdDaysAgo: 66, dueDaysAfterCreation: 7)
            .AssignTo(technicianId).Start(daysAgo: 65)
            .Work("Wymieniono toner czarny, wyczyszczono zespół ładujący i wykonano kalibrację gęstości.", startedDaysAgo: 65, hours: 1, [TonerCartridgePhotoUrl], ("TNR-RIC-C3000-K", 1))
            .Complete(daysAgo: 64.95).Invoice(daysAgo: 62);
        Open("HP-M528DN-CNB2K4101", "Zablokowany moduł dupleksu, komunikat 13.B9.", WorkOrderPriority.Normal, createdDaysAgo: 62, dueDaysAfterCreation: 3)
            .AssignTo(technicianId).Start(daysAgo: 61)
            .Work("Usunięto zacięty fragment papieru z modułu dupleksu, wyczyszczono czujniki toru papieru.", startedDaysAgo: 61, hours: 0.5)
            .Complete(daysAgo: 60.95).Invoice(daysAgo: 58);
    }

    private void AddUnassignedWorkOrders()
    {
        Open("SAM-AR12TXEA-5520411", "Coroczny przegląd klimatyzacji wraz z odgrzybianiem.", WorkOrderPriority.Low, createdDaysAgo: 0.3, dueDaysAfterCreation: 14);
        Open("KM-C450I-6610032", "Wymiana tonerów i kalibracja kolorów przed dużym zleceniem.", WorkOrderPriority.Normal, createdDaysAgo: 0.2, dueDaysAfterCreation: 3);
        Open("LG-ARUM100-4410298", "Jednostka zewnętrzna zgłasza błąd sprężarki, brak chłodzenia w całym hotelu.", WorkOrderPriority.Critical, createdDaysAgo: 0.05, dueDaysAfterCreation: 0.5);
        Open("CAN-IR2425-2271840", "Aktualizacja oprogramowania układowego urządzenia.", WorkOrderPriority.Low, createdDaysAgo: 10, dueDaysAfterCreation: 7);
    }

    private WorkOrderTimeline Open(
        string serialNumber,
        string description,
        WorkOrderPriority priority,
        double createdDaysAgo,
        double dueDaysAfterCreation)
    {
        var device = _inventory.DeviceWithSerialNumber(serialNumber);
        var createdAt = DaysAgo(createdDaysAgo);
        var dueDate = (createdAt + TimeSpan.FromDays(dueDaysAfterCreation)).ToDatabasePrecision();
        var workOrder = Expect(WorkOrder.Create(device, description, priority, dueDate, createdAt));
        _workOrders.Add(workOrder);
        return new WorkOrderTimeline(this, workOrder, LocationOf(device));
    }

    private GpsLocation LocationOf(Device device) =>
        LocationsByCity[_inventory.Clients.Single(client => client.Id == device.ClientId).Address.City];

    private DateTimeOffset DaysAgo(double days) => (_now - TimeSpan.FromDays(days)).ToDatabasePrecision();

    private List<PartUsage> UsagesOf((string CatalogNumber, int Quantity)[] parts) =>
        parts.Select(part => new PartUsage(_inventory.PartWithCatalogNumber(part.CatalogNumber), part.Quantity)).ToList();

    private static T Expect<T>(ErrorOr<T> result) =>
        result.IsError
            ? throw new InvalidOperationException($"Demo work order scenario failed: {result.FirstError.Description}")
            : result.Value;

    private sealed class WorkOrderTimeline(DemoWorkOrderHistory history, WorkOrder workOrder, GpsLocation location)
    {
        private readonly List<ServiceEntry> _entries = [];

        private Guid TechnicianId => workOrder.TechnicianId
            ?? throw new InvalidOperationException("Demo work order scenario requires an assigned technician.");

        public WorkOrderTimeline AssignTo(Guid technicianId)
        {
            Expect(workOrder.Assign(technicianId));
            return this;
        }

        public WorkOrderTimeline Start(double daysAgo)
        {
            Expect(workOrder.Start(TechnicianId, technicianHasWorkInProgress: false, history.DaysAgo(daysAgo)));
            return this;
        }

        public WorkOrderTimeline Work(string note, double startedDaysAgo, double hours, params (string CatalogNumber, int Quantity)[] parts) =>
            Work(note, startedDaysAgo, hours, [], parts);

        public WorkOrderTimeline Work(
            string note,
            double startedDaysAgo,
            double hours,
            IReadOnlyList<string> photoUrls,
            params (string CatalogNumber, int Quantity)[] parts)
        {
            var startedAt = history.DaysAgo(startedDaysAgo);
            var finishedAt = (startedAt + TimeSpan.FromHours(hours)).ToDatabasePrecision();
            return Record(Expect(ServiceEntry.CreateWork(
                workOrder,
                TechnicianId,
                note,
                photoUrls,
                startedAt,
                finishedAt,
                location,
                history.UsagesOf(parts),
                finishedAt + EntryRecordingDelay)));
        }

        public WorkOrderTimeline Correction(string note, double daysAgo, params (string CatalogNumber, int Quantity)[] parts) =>
            Record(Expect(ServiceEntry.CreateCorrection(
                workOrder,
                TechnicianId,
                note,
                [],
                history.UsagesOf(parts),
                _entries,
                history.DaysAgo(daysAgo))));

        public WorkOrderTimeline Complete(double daysAgo)
        {
            Expect(workOrder.Complete(hasServiceEntries: _entries.Count > 0, history.DaysAgo(daysAgo)));
            return this;
        }

        public WorkOrderTimeline Invoice(double daysAgo)
        {
            Expect(workOrder.Invoice(history.DaysAgo(daysAgo)));
            return this;
        }

        private WorkOrderTimeline Record(ServiceEntry entry)
        {
            _entries.Add(entry);
            history._serviceEntries.Add(entry);
            return this;
        }
    }
}
