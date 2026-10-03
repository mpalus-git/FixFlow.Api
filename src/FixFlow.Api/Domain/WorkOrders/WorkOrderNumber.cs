using System.Globalization;

namespace FixFlow.Api.Domain.WorkOrders;

public static class WorkOrderNumber
{
    public const int MaxLength = 20;

    public static string Format(int year, int sequence) =>
        string.Create(CultureInfo.InvariantCulture, $"ZL/{year}/{sequence:D4}");
}
