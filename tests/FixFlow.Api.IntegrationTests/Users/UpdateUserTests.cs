using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Users;
using FixFlow.Api.Features.Users.UpdateUser;

namespace FixFlow.Api.IntegrationTests.Users;

public sealed class UpdateUserTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Change_Full_Name_Only_When_Admin_Updates_User()
    {
        using var adminClient = await CreateAuthenticatedClientAsync(Roles.Admin);
        var technician = await CreateUserAsync(Roles.Technician, "Jan Kowalsky");

        using var response = await adminClient.PutAsJsonAsync(UserUri(technician.Id), new UpdateUserRequest(" Jan Kowalski "), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updatedUser = await response.Content.ReadFromJsonAsync<UserResponse>(TestContext.Current.CancellationToken);
        updatedUser.ShouldBe(new UserResponse(technician.Id, technician.Email, "Jan Kowalski", Roles.Technician, IsActive: true));
        using var technicianClient = await CreateAuthenticatedClientAsync(technician);
        var currentUser = await technicianClient.GetFromJsonAsync<UserResponse>(new Uri("/api/v1/users/me", UriKind.Relative), TestContext.Current.CancellationToken);
        currentUser.ShouldBe(updatedUser);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Full_Name_Is_Empty()
    {
        using var adminClient = await CreateAuthenticatedClientAsync(Roles.Admin);
        var dispatcher = await CreateUserAsync(Roles.Dispatcher);

        using var response = await adminClient.PutAsJsonAsync(UserUri(dispatcher.Id), new UpdateUserRequest(" "), TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("fullName");
    }

    private static Uri UserUri(Guid userId) => new($"/api/v1/users/{userId}", UriKind.Relative);
}
