using System.Globalization;
using FixFlow.Api.Common.Pdf;
using FixFlow.Api.Common.Time;
using FixFlow.Api.Domain.ServiceEntries;
using FixFlow.Api.Domain.WorkOrders;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FixFlow.Api.Features.WorkOrders.GetServiceProtocol;

public sealed class ServiceProtocolDocument(ServiceProtocol protocol) : IDocument
{
    private static readonly NumberFormatInfo PolishNumberFormat = new() { NumberDecimalSeparator = ",", NumberGroupSeparator = " " };

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Protokół serwisowy {protocol.WorkOrder.Id}",
        Author = "FixFlow",
        Language = "pl-PL",
        CreationDate = protocol.IssuedAt,
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.8f, Unit.Centimetre);
            page.DefaultTextStyle(style => style.FontFamily(PdfGeneration.FontFamily).FontSize(9.5f));
            page.Header().Element(ComposeHeader);
            page.Content().PaddingVertical(12).Column(column =>
            {
                column.Spacing(14);
                column.Item().Element(ComposeWorkOrder);
                column.Item().Row(row =>
                {
                    row.Spacing(20);
                    row.RelativeItem().Element(ComposeClient);
                    row.RelativeItem().Element(ComposeDevice);
                });
                column.Item().Element(ComposeServiceEntries);
                column.Item().Element(ComposeUsedParts);
                column.Item().PaddingTop(30).Element(ComposeSignatures);
            });
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Strona ");
                text.CurrentPageNumber();
                text.Span(" z ");
                text.TotalPages();
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.BorderBottom(1).BorderColor(Colors.Grey.Medium).PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Protokół serwisowy").FontSize(18).Bold();
                column.Item().Text($"Zlecenie {protocol.WorkOrder.Id}").FontColor(Colors.Grey.Darken2);
            });
            row.AutoItem().AlignBottom().Text($"Wystawiono: {FormatDateTime(protocol.IssuedAt)}");
        });
    }

    private void ComposeWorkOrder(IContainer container)
    {
        var workOrder = protocol.WorkOrder;
        container.Column(column =>
        {
            column.Item().Element(SectionTitle).Text("Zlecenie");
            column.Item().Element(container => Field(container, "Opis usterki", workOrder.Description));
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Element(container => Field(container, "Priorytet", DescribePriority(workOrder.Priority)));
                    left.Item().Element(container => Field(container, "Status", DescribeStatus(workOrder.Status)));
                    left.Item().Element(container => Field(container, "Technik", protocol.TechnicianName ?? "-"));
                });
                row.RelativeItem().Column(right =>
                {
                    right.Item().Element(container => Field(container, "Termin", FormatDateTime(workOrder.DueDate)));
                    right.Item().Element(container => Field(container, "Rozpoczęto", FormatDateTime(workOrder.StartedAt)));
                    right.Item().Element(container => Field(container, "Zakończono", FormatDateTime(workOrder.CompletedAt)));
                });
            });
        });
    }

    private void ComposeClient(IContainer container)
    {
        var client = protocol.Client;
        var address = client.Address;
        container.Column(column =>
        {
            column.Item().Element(SectionTitle).Text("Klient");
            column.Item().Text(client.Name).Bold();
            column.Item().Text($"{address.Street} {address.BuildingNumber}");
            column.Item().Text($"{address.PostalCode} {address.City}");
            column.Item().Element(container => Field(container, "Osoba kontaktowa", client.ContactPerson));
            column.Item().Element(container => Field(container, "Telefon", client.Phone));
            column.Item().Element(container => Field(container, "E-mail", client.Email ?? "-"));
        });
    }

    private void ComposeDevice(IContainer container)
    {
        var device = protocol.Device;
        container.Column(column =>
        {
            column.Item().Element(SectionTitle).Text("Urządzenie");
            column.Item().Element(container => Field(container, "Numer seryjny", device.SerialNumber));
            column.Item().Element(container => Field(container, "Model", device.Model));
            column.Item().Element(container => Field(container, "Producent", device.Manufacturer));
            column.Item().Element(container => Field(container, "Data instalacji", device.InstallationDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)));
        });
    }

    private void ComposeServiceEntries(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(SectionTitle).Text("Wpisy serwisowe");
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(70);
                    columns.ConstantColumn(55);
                    columns.ConstantColumn(115);
                    columns.RelativeColumn();
                    columns.ConstantColumn(40);
                });
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Data");
                    header.Cell().Element(HeaderCell).Text("Rodzaj");
                    header.Cell().Element(HeaderCell).Text("Czas pracy");
                    header.Cell().Element(HeaderCell).Text("Notatka");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Zdjęcia");
                });
                foreach (var entry in protocol.ServiceEntries)
                {
                    table.Cell().Element(BodyCell).Text(FormatDate(entry.WorkStartedAt ?? entry.CreatedAt));
                    table.Cell().Element(BodyCell).Text(entry.IsCorrection ? "Korekta" : "Praca");
                    table.Cell().Element(BodyCell).Text(DescribeWorkTime(entry));
                    table.Cell().Element(BodyCell).Text(entry.Note);
                    table.Cell().Element(BodyCell).AlignRight().Text(entry.PhotoUrls.Count.ToString(CultureInfo.InvariantCulture));
                }
            });
            column.Item().PaddingTop(4).AlignRight().Text(text =>
            {
                text.Span("Łączny czas pracy: ");
                text.Span(FormatDuration(protocol.TotalWorkTime)).Bold();
            });
        });
    }

    private void ComposeUsedParts(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(SectionTitle).Text("Zużyte części");
            if (protocol.PartLines.Count == 0)
            {
                column.Item().Text("Brak zużytych części.");
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.ConstantColumn(45);
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(85);
                });
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Nazwa");
                    header.Cell().Element(HeaderCell).Text("Nr katalogowy");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Ilość");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Cena jedn.");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Wartość");
                });
                foreach (var line in protocol.PartLines)
                {
                    table.Cell().Element(BodyCell).Text(line.Name);
                    table.Cell().Element(BodyCell).Text(line.CatalogNumber);
                    table.Cell().Element(BodyCell).AlignRight().Text(line.Quantity.ToString(CultureInfo.InvariantCulture));
                    table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(line.UnitPrice));
                    table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(line.Value));
                }
            });
            column.Item().PaddingTop(4).AlignRight().Text(text =>
            {
                text.Span("Razem: ");
                text.Span(FormatMoney(protocol.PartsTotal)).Bold();
            });
        });
    }

    private static void ComposeSignatures(IContainer container)
    {
        container.Row(row =>
        {
            row.Spacing(60);
            row.RelativeItem().PaddingTop(35).BorderTop(1).AlignCenter().Text("Podpis technika");
            row.RelativeItem().PaddingTop(35).BorderTop(1).AlignCenter().Text("Podpis klienta");
        });
    }

    private static IContainer SectionTitle(IContainer container) =>
        container.PaddingBottom(4).DefaultTextStyle(style => style.FontSize(12).Bold());

    private static IContainer HeaderCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Medium).PaddingVertical(3).PaddingRight(4).DefaultTextStyle(style => style.Bold());

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingRight(4);

    private static void Field(IContainer container, string label, string value) =>
        container.Text(text =>
        {
            text.Span($"{label}: ").Bold();
            text.Span(value);
        });

    private static string DescribeWorkTime(ServiceEntry entry) =>
        entry is { WorkStartedAt: { } startedAt, WorkFinishedAt: { } finishedAt }
            ? $"{FormatTime(startedAt)}–{FormatTime(finishedAt)} ({FormatDuration(finishedAt - startedAt)})"
            : "-";

    private static string DescribePriority(WorkOrderPriority priority) => priority switch
    {
        WorkOrderPriority.Low => "Niski",
        WorkOrderPriority.Normal => "Normalny",
        WorkOrderPriority.High => "Wysoki",
        WorkOrderPriority.Critical => "Krytyczny",
        _ => priority.ToString(),
    };

    private static string DescribeStatus(WorkOrderStatus status) => status switch
    {
        WorkOrderStatus.Completed => "Zakończone",
        WorkOrderStatus.Invoiced => "Zafakturowane",
        _ => status.ToString(),
    };

    private static string FormatMoney(decimal amount) => $"{amount.ToString("N2", PolishNumberFormat)} PLN";

    private static string FormatDuration(TimeSpan duration) => $"{(int)duration.TotalHours} h {duration.Minutes:00} min";

    private static string FormatDate(DateTimeOffset moment) =>
        BusinessTime.From(moment).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    private static string FormatTime(DateTimeOffset moment) =>
        BusinessTime.From(moment).ToString("HH:mm", CultureInfo.InvariantCulture);

    private static string FormatDateTime(DateTimeOffset? moment) =>
        moment is { } value ? BusinessTime.From(value).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) : "-";
}
