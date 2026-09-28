using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Users;
using FixFlow.Api.Features.Users.CreateUser;
using FixFlow.Api.IntegrationTests.Auth;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FixFlow.Api.IntegrationTests.Users;

public sealed class CreateUserTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly Uri UsersUri = new("/api/v1/users", UriKind.Relative);

    [Fact]
    public async Task Should_Create_Account_That_Can_Log_In_With_Role_When_Admin_Creates_It()
    {
        using var adminClient = await CreateAuthenticatedClientAsync(Roles.Admin);
        var request = new CreateUserRequest("new.technician@fixflow.test", "Initial1!password", Roles.Technician);

        using var response = await adminClient.PostAsJsonAsync(UsersUri, request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var createdUser = await response.Content.ReadFromJsonAsync<UserResponse>(TestContext.Current.CancellationToken);
        createdUser.ShouldNotBeNull();
        createdUser.Email.ShouldBe(request.Email);
        createdUser.Role.ShouldBe(Roles.Technician);
        using var anonymousClient = Factory.CreateClient();
        var tokens = await anonymousClient.LoginAsync(new TestUser(createdUser.Id, request.Email, request.Password));
        var accessToken = new JsonWebTokenHandler().ReadJsonWebToken(tokens.AccessToken);
        accessToken.Subject.ShouldBe(createdUser.Id.ToString());
        accessToken.GetClaim(AuthClaimTypes.Role).Value.ShouldBe(Roles.Technician);
    }

    [Fact]
    public async Task Should_Return_Unauthorized_When_Request_Has_No_Access_Token()
    {
        using var client = Factory.CreateClient();

        using var response = await client.PostAsJsonAsync(UsersUri, new CreateUserRequest("someone@fixflow.test", "Initial1!password", Roles.Technician), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Email_Is_Already_Used()
    {
        var existingUser = await CreateUserAsync(Roles.Dispatcher);
        using var adminClient = await CreateAuthenticatedClientAsync(Roles.Admin);

        using var response = await adminClient.PostAsJsonAsync(UsersUri, new CreateUserRequest(existingUser.Email, "Initial1!password", Roles.Technician), TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, UserErrors.DuplicateEmail.Code);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_And_Not_Create_Account_When_Password_Does_Not_Meet_Policy()
    {
        using var adminClient = await CreateAuthenticatedClientAsync(Roles.Admin);
        var request = new CreateUserRequest("weak.password@fixflow.test", "alllowercase1", Roles.Technician);

        using var response = await adminClient.PostAsJsonAsync(UsersUri, request, TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("password");
        using var anonymousClient = Factory.CreateClient();
        using var loginResponse = await anonymousClient.PostAsJsonAsync(
            AuthRequests.LoginUri,
            new Features.Auth.Login.LoginRequest(request.Email, request.Password),
            TestContext.Current.CancellationToken);
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Role_Is_Unknown()
    {
        using var adminClient = await CreateAuthenticatedClientAsync(Roles.Admin);

        using var response = await adminClient.PostAsJsonAsync(UsersUri, new CreateUserRequest("someone@fixflow.test", "Initial1!password", "Manager"), TestContext.Current.CancellationToken);

        await response.ShouldBeValidationProblemAsync("role");
    }
}
