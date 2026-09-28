using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Domain.WorkOrders;
using FixFlow.Api.Features.Auth.Login;
using FixFlow.Api.Features.Users;
using FixFlow.Api.IntegrationTests.Auth;
using FixFlow.Api.IntegrationTests.WorkOrders;

namespace FixFlow.Api.IntegrationTests.Users;

public sealed class AccountActivationTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Block_Sign_In_And_Token_Refresh_When_Admin_Deactivates_Account()
    {
        using var adminClient = await CreateAuthenticatedClientAsync(Roles.Admin);
        var technician = await CreateUserAsync(Roles.Technician);
        var technicianTokens = await adminClient.LoginAsync(technician);

        using var response = await adminClient.PostAsync(DeactivateUri(technician.Id), null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var loginResponse = await PostLoginAsync(adminClient, technician);
        using var refreshResponse = await adminClient.PostRefreshAsync(technicianTokens.RefreshToken);
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_Allow_Sign_In_Again_When_Deactivated_Account_Is_Activated()
    {
        using var adminClient = await CreateAuthenticatedClientAsync(Roles.Admin);
        var dispatcher = await CreateUserAsync(Roles.Dispatcher);
        await DeactivateUserDirectlyAsync(dispatcher.Id);

        using var response = await adminClient.PostAsync(ActivateUri(dispatcher.Id), null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var loginResponse = await PostLoginAsync(adminClient, dispatcher);
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Technician_Has_Assigned_Work_Order()
    {
        using var adminClient = await CreateAuthenticatedClientAsync(Roles.Admin);
        var technician = await CreateUserAsync(Roles.Technician);
        var workOrder = await adminClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await adminClient.CreateServicedDeviceAsync()));
        await Factory.AssignTechnicianDirectlyAsync(workOrder.Id, technician.Id);

        using var response = await adminClient.PostAsync(DeactivateUri(technician.Id), null, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, UserErrors.HasOpenWorkOrders.Code);
        using var loginResponse = await PostLoginAsync(adminClient, technician);
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Admin_Deactivates_Own_Account()
    {
        var admin = await CreateUserAsync(Roles.Admin);
        using var adminClient = await CreateAuthenticatedClientAsync(admin);

        using var response = await adminClient.PostAsync(DeactivateUri(admin.Id), null, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, UserErrors.CannotDeactivateSelf.Code);
    }

    [Fact]
    public async Task Should_Reject_Assignment_When_Technician_Is_Deactivated()
    {
        using var dispatcherClient = await CreateAuthenticatedClientAsync(Roles.Dispatcher);
        var technician = await CreateUserAsync(Roles.Technician);
        await DeactivateUserDirectlyAsync(technician.Id);
        var workOrder = await dispatcherClient.CreateWorkOrderAsync(WorkOrderRequests.NewWorkOrder(await dispatcherClient.CreateServicedDeviceAsync()));

        using var response = await dispatcherClient.PostAssignAsync(workOrder.Id, technician.Id);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, WorkOrderErrors.TechnicianNotFound.Code);
    }

    private static Uri DeactivateUri(Guid userId) => new($"/api/v1/users/{userId}/deactivate", UriKind.Relative);

    private static Uri ActivateUri(Guid userId) => new($"/api/v1/users/{userId}/activate", UriKind.Relative);

    private static Task<HttpResponseMessage> PostLoginAsync(HttpClient client, TestUser user) =>
        client.PostAsJsonAsync(AuthRequests.LoginUri, new LoginRequest(user.Email, user.Password), TestContext.Current.CancellationToken);
}
