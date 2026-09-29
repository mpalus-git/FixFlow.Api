using ErrorOr;

namespace FixFlow.Api.Features.DemoData;

public static class DemoDataErrors
{
    public static readonly Error Disabled = Error.Conflict("DemoData.Disabled", "Demo data is disabled on this instance.");
}
