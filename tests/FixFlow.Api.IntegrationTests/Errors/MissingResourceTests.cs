using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Clients;
using FixFlow.Api.Domain.Devices;
using FixFlow.Api.Domain.Parts;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.Clients;
using FixFlow.Api.Features.Clients.UpdateClient;
using FixFlow.Api.Features.Devices.UpdateDevice;
using FixFlow.Api.Features.Parts.RestockPart;
using FixFlow.Api.Features.Parts.UpdatePart;
using FixFlow.Api.Features.Users;
using FixFlow.Api.Features.Users.ResetPassword;
using FixFlow.Api.Features.ServiceEntries.AddServiceEntry;
using FixFlow.Api.Features.WorkOrders.AssignTechnician;
using FixFlow.Api.Features.WorkOrders.UpdateWorkOrder;
using FixFlow.Api.IntegrationTests.Devices;
using FixFlow.Api.IntegrationTests.WorkOrders;

namespace FixFlow.Api.IntegrationTests.Errors;

public sealed class MissingResourceTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private const string MissingId = "{missingId}";

    private static readonly Dictionary<string, MissingResourceRequest> Requests = new()
    {
        ["get client"] = new(Roles.Technician, HttpMethod.Get, $"clients/{MissingId}", null, ClientErrors.NotFound.Code),
        ["update client"] = new(
            Roles.Dispatcher,
            HttpMethod.Put,
            $"clients/{MissingId}",
            new UpdateClientRequest("Klimat-Serwis", new ClientAddress("Przemysłowa", "3/1", "30-701", "Kraków"), "Jan Kowalski", "+48 600 300 400"),
            ClientErrors.NotFound.Code),
        ["archive client"] = new(Roles.Dispatcher, HttpMethod.Post, $"clients/{MissingId}/archive", null, ClientErrors.NotFound.Code),
        ["create device of missing client"] = new(Roles.Dispatcher, HttpMethod.Post, "devices", DeviceRequests.NewDevice(Guid.CreateVersion7()), ClientErrors.NotFound.Code),
        ["get device"] = new(Roles.Technician, HttpMethod.Get, $"devices/{MissingId}", null, DeviceErrors.NotFound.Code),
        ["update device"] = new(
            Roles.Dispatcher,
            HttpMethod.Put,
            $"devices/{MissingId}",
            new UpdateDeviceRequest("AC-2002", "Multi 5 kW", "Mitsubishi", new DateOnly(2025, 3, 10)),
            DeviceErrors.NotFound.Code),
        ["archive device"] = new(Roles.Dispatcher, HttpMethod.Post, $"devices/{MissingId}/archive", null, DeviceErrors.NotFound.Code),
        ["get part"] = new(Roles.Technician, HttpMethod.Get, $"parts/{MissingId}", null, PartErrors.NotFound.Code),
        ["update part"] = new(Roles.Dispatcher, HttpMethod.Put, $"parts/{MissingId}", new UpdatePartRequest("Filtr węglowy", "FLT-200", 59.50m), PartErrors.NotFound.Code),
        ["archive part"] = new(Roles.Dispatcher, HttpMethod.Post, $"parts/{MissingId}/archive", null, PartErrors.NotFound.Code),
        ["restock part"] = new(Roles.Dispatcher, HttpMethod.Post, $"parts/{MissingId}/restock", new RestockPartRequest(1), PartErrors.NotFound.Code),
        ["create work order of missing device"] = new(Roles.Dispatcher, HttpMethod.Post, "work-orders", WorkOrderRequests.NewWorkOrder(Guid.CreateVersion7()), DeviceErrors.NotFound.Code),
        ["get work order"] = new(Roles.Dispatcher, HttpMethod.Get, $"work-orders/{MissingId}", null, WorkOrderErrors.NotFound.Code),
        ["update work order"] = new(
            Roles.Dispatcher,
            HttpMethod.Put,
            $"work-orders/{MissingId}",
            new UpdateWorkOrderRequest("Changed", WorkOrderPriority.Low, DateTimeOffset.UtcNow.AddDays(1)),
            WorkOrderErrors.NotFound.Code),
        ["assign technician"] = new(
            Roles.Dispatcher,
            HttpMethod.Post,
            $"work-orders/{MissingId}/assign",
            new AssignTechnicianRequest(Guid.CreateVersion7()),
            WorkOrderErrors.NotFound.Code),
        ["unassign technician"] = new(Roles.Dispatcher, HttpMethod.Post, $"work-orders/{MissingId}/unassign", null, WorkOrderErrors.NotFound.Code),
        ["start work"] = new(Roles.Technician, HttpMethod.Post, $"work-orders/{MissingId}/start", null, WorkOrderErrors.NotFound.Code),
        ["complete work order"] = new(Roles.Dispatcher, HttpMethod.Post, $"work-orders/{MissingId}/complete", null, WorkOrderErrors.NotFound.Code),
        ["invoice work order"] = new(Roles.Dispatcher, HttpMethod.Post, $"work-orders/{MissingId}/invoice", null, WorkOrderErrors.NotFound.Code),
        ["get service protocol"] = new(Roles.Dispatcher, HttpMethod.Get, $"work-orders/{MissingId}/protocol", null, WorkOrderErrors.NotFound.Code),
        ["add service entry"] = new(
            Roles.Technician,
            HttpMethod.Post,
            $"work-orders/{MissingId}/service-entries",
            new AddServiceEntryRequest("Replaced filters", WorkStartedAt: DateTimeOffset.UtcNow.AddHours(-2), WorkFinishedAt: DateTimeOffset.UtcNow.AddHours(-1)),
            WorkOrderErrors.NotFound.Code),
        ["deactivate user"] = new(Roles.Admin, HttpMethod.Post, $"users/{MissingId}/deactivate", null, UserErrors.NotFound.Code),
        ["activate user"] = new(Roles.Admin, HttpMethod.Post, $"users/{MissingId}/activate", null, UserErrors.NotFound.Code),
        ["reset password"] = new(Roles.Admin, HttpMethod.Post, $"users/{MissingId}/password", new ResetPasswordRequest("NewSecret1!"), UserErrors.NotFound.Code),
        ["list service entries"] = new(Roles.Dispatcher, HttpMethod.Get, $"work-orders/{MissingId}/service-entries", null, WorkOrderErrors.NotFound.Code),
    };

    public static TheoryData<string> RequestNames { get; } = [.. Requests.Keys];

    [Theory]
    [MemberData(nameof(RequestNames))]
    public async Task Should_Return_Not_Found_Problem_When_Requested_Resource_Does_Not_Exist(string requestName)
    {
        var missingResourceRequest = Requests[requestName];
        using var client = await CreateAuthenticatedClientAsync(missingResourceRequest.Role);
        using var request = new HttpRequestMessage(
            missingResourceRequest.Method,
            new Uri($"/api/v1/{missingResourceRequest.Path.Replace(MissingId, Guid.CreateVersion7().ToString(), StringComparison.Ordinal)}", UriKind.Relative));
        request.Headers.TryAddWithoutValidation("If-Match", ConditionalRequests.AnyVersion);
        if (missingResourceRequest.Body is not null)
        {
            request.Content = JsonContent.Create(missingResourceRequest.Body, missingResourceRequest.Body.GetType(), options: ApiJson.Options);
        }

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, missingResourceRequest.ErrorCode);
    }

    private sealed record MissingResourceRequest(string Role, HttpMethod Method, string Path, object? Body, string ErrorCode);
}
