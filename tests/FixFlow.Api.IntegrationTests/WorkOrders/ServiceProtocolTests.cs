using System.Net;
using System.Text;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders.GetServiceProtocol;
using FixFlow.Api.IntegrationTests.ServiceEntries;

namespace FixFlow.Api.IntegrationTests.WorkOrders;

public sealed class ServiceProtocolTests(FixFlowApiFactory factory) : ServiceEntryTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Pdf_Protocol_With_Embedded_Font_When_Dispatcher_Downloads_Completed_Work_Order()
    {
        using var scenario = await CreateCompletedWorkOrderAsync();

        using var response = await GetProtocolAsync(scenario.DispatcherClient, scenario.WorkOrder.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(GetServiceProtocolEndpoint.PdfContentType);
        response.Content.Headers.ContentDisposition?.FileName.ShouldBe($"protokol-{scenario.WorkOrder.Id}.pdf");
        var pdf = Encoding.Latin1.GetString(await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        pdf.ShouldStartWith("%PDF");
        pdf.ShouldContain("+NotoSans-Regular");
        pdf.ShouldContain("+NotoSans-Bold");
    }

    [Fact]
    public async Task Should_Return_Pdf_Protocol_When_Assigned_Technician_Downloads_Invoiced_Work_Order()
    {
        using var scenario = await CreateCompletedWorkOrderAsync();
        using var invoiceResponse = await scenario.DispatcherClient.PostTransitionAsync(scenario.WorkOrder.Id, "invoice");
        invoiceResponse.EnsureSuccessStatusCode();

        using var response = await GetProtocolAsync(scenario.TechnicianClient, scenario.WorkOrder.Id);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(GetServiceProtocolEndpoint.PdfContentType);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Work_Order_Is_Not_Completed()
    {
        using var scenario = await CreateWorkOrderInProgressWithServiceEntryAsync();

        using var response = await GetProtocolAsync(scenario.DispatcherClient, scenario.WorkOrder.Id);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, WorkOrderErrors.NotCompleted.Code);
    }

    [Fact]
    public async Task Should_Return_Not_Found_Problem_When_Technician_Downloads_Protocol_Of_Another_Technician()
    {
        using var scenario = await CreateCompletedWorkOrderAsync();
        using var otherTechnicianClient = await CreateAuthenticatedClientAsync(Roles.Technician);

        using var response = await GetProtocolAsync(otherTechnicianClient, scenario.WorkOrder.Id);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, WorkOrderErrors.NotFound.Code);
    }

    private static Task<HttpResponseMessage> GetProtocolAsync(HttpClient client, Guid workOrderId) =>
        client.GetAsync(new Uri($"/api/v1/work-orders/{workOrderId}/protocol", UriKind.Relative), TestContext.Current.CancellationToken);
}
