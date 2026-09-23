using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Auth;
using FixFlow.Api.Features.Auth.Login;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FixFlow.Api.IntegrationTests.Auth;

public sealed class LoginTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly Uri LoginUri = new("/api/v1/auth/login", UriKind.Relative);

    [Fact]
    public async Task Should_Return_Access_And_Refresh_Tokens_When_Credentials_Are_Valid()
    {
        var user = await CreateUserAsync(Roles.Dispatcher);
        using var client = Factory.CreateClient();

        using var response = await client.PostAsJsonAsync(LoginUri, new LoginRequest(user.Email, user.Password), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tokens = await response.Content.ReadFromJsonAsync<AuthTokensResponse>(TestContext.Current.CancellationToken);
        tokens.ShouldNotBeNull();
        var accessToken = new JsonWebTokenHandler().ReadJsonWebToken(tokens.AccessToken);
        accessToken.Subject.ShouldBe(user.Id.ToString());
        accessToken.GetClaim(AuthClaimTypes.Role).Value.ShouldBe(Roles.Dispatcher);
        tokens.RefreshTokenExpiresAt.ShouldBeGreaterThan(tokens.AccessTokenExpiresAt);
    }

    [Fact]
    public async Task Should_Store_Only_Hash_Of_Refresh_Token_When_Login_Succeeds()
    {
        var user = await CreateUserAsync(Roles.Technician);
        using var client = Factory.CreateClient();

        using var response = await client.PostAsJsonAsync(LoginUri, new LoginRequest(user.Email, user.Password), TestContext.Current.CancellationToken);

        var tokens = await response.Content.ReadFromJsonAsync<AuthTokensResponse>(TestContext.Current.CancellationToken);
        tokens.ShouldNotBeNull();
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var storedToken = await dbContext.RefreshTokens.SingleAsync(TestContext.Current.CancellationToken);
        storedToken.UserId.ShouldBe(user.Id);
        storedToken.TokenHash.ShouldBe(RefreshTokenSecret.Hash(tokens.RefreshToken));
        storedToken.TokenHash.ShouldNotBe(tokens.RefreshToken);
    }

    [Fact]
    public async Task Should_Return_Unauthorized_Problem_When_Password_Is_Wrong()
    {
        var user = await CreateUserAsync(Roles.Technician);
        using var client = Factory.CreateClient();

        using var response = await client.PostAsJsonAsync(LoginUri, new LoginRequest(user.Email, "Wrong1!password"), TestContext.Current.CancellationToken);

        await ShouldBeInvalidCredentialsProblemAsync(response);
    }

    [Fact]
    public async Task Should_Return_Same_Unauthorized_Problem_When_User_Does_Not_Exist()
    {
        using var client = Factory.CreateClient();

        using var response = await client.PostAsJsonAsync(LoginUri, new LoginRequest("nobody@fixflow.test", "Some1!password"), TestContext.Current.CancellationToken);

        await ShouldBeInvalidCredentialsProblemAsync(response);
    }

    [Fact]
    public async Task Should_Reject_Valid_Password_When_Account_Is_Locked_Out()
    {
        var user = await CreateUserAsync(Roles.Technician);
        using var client = Factory.CreateClient();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failedResponse = await client.PostAsJsonAsync(LoginUri, new LoginRequest(user.Email, "Wrong1!password"), TestContext.Current.CancellationToken);
        }

        using var response = await client.PostAsJsonAsync(LoginUri, new LoginRequest(user.Email, user.Password), TestContext.Current.CancellationToken);

        await ShouldBeInvalidCredentialsProblemAsync(response);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Email_Is_Not_Valid()
    {
        using var client = Factory.CreateClient();

        using var response = await client.PostAsJsonAsync(LoginUri, new LoginRequest("not-an-email", "Some1!password"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var problem = await ReadJsonAsync(response);
        problem.RootElement.GetProperty("errors").TryGetProperty("Email", out _).ShouldBeTrue();
    }

    private static async Task ShouldBeInvalidCredentialsProblemAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var problem = await ReadJsonAsync(response);
        problem.RootElement.GetProperty("errorCode").GetString().ShouldBe(AuthErrors.InvalidCredentials.Code);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);
    }
}
