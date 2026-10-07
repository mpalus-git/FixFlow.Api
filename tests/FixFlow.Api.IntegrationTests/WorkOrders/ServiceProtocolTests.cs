using System.Net;
using System.Text;
using FixFlow.Api.Common.Time;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.WorkOrders.CompleteWorkOrder;
using FixFlow.Api.Features.WorkOrders.GetServiceProtocol;
using FixFlow.Api.IntegrationTests.Photos;
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
        response.Content.Headers.ContentDisposition?.FileName.ShouldBe($"protokol-ZL-{BusinessTime.From(scenario.WorkOrder.CreatedAt).Year}-0001.pdf");
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
    public async Task Should_Embed_Client_Signature_In_Protocol_When_Work_Order_Was_Completed_With_Signature()
    {
        using var scenario = await CreateWorkOrderInProgressWithServiceEntryAsync();
        var signaturePhotoId = Guid.CreateVersion7();
        using var uploadResponse = await scenario.TechnicianClient.PutPhotoAsync(signaturePhotoId, PhotoRequests.ReadableJpeg());
        using var completeResponse = await scenario.TechnicianClient.PostCompleteAsync(scenario.WorkOrder.Id, new CompleteWorkOrderRequest(ClientSignaturePhotoId: signaturePhotoId));

        using var response = await GetProtocolAsync(scenario.DispatcherClient, scenario.WorkOrder.Id);

        completeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadPdfAsync(response)).ShouldContain("/Subtype /Image");
    }

    [Fact]
    public async Task Should_Leave_Client_Signature_Empty_In_Protocol_When_Work_Order_Was_Completed_Without_Signature()
    {
        using var scenario = await CreateCompletedWorkOrderAsync();

        using var response = await GetProtocolAsync(scenario.DispatcherClient, scenario.WorkOrder.Id);

        (await ReadPdfAsync(response)).ShouldNotContain("/Subtype /Image");
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Work_Order_Is_Not_Completed()
    {
        using var scenario = await CreateWorkOrderInProgressWithServiceEntryAsync();

        using var response = await GetProtocolAsync(scenario.DispatcherClient, scenario.WorkOrder.Id);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, WorkOrderErrors.NotCompleted.Code);
    }

    private static async Task<string> ReadPdfAsync(HttpResponseMessage response) =>
        Encoding.Latin1.GetString(await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));

    private static Task<HttpResponseMessage> GetProtocolAsync(HttpClient client, Guid workOrderId) =>
        client.GetAsync(new Uri($"/api/v1/work-orders/{workOrderId}/protocol", UriKind.Relative), TestContext.Current.CancellationToken);
}
