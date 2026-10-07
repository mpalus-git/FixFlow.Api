using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Auth;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Auth;
using FixFlow.Api.Features.Auth.Logout;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests.Auth;

public sealed class RefreshTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_New_Token_Pair_And_Revoke_Previous_Token_When_Refresh_Token_Is_Active()
    {
        var user = await CreateUserAsync(Roles.Technician);
        using var client = Factory.CreateClient();
        var loginTokens = await client.LoginAsync(user);

        using var response = await client.PostRefreshAsync(loginTokens.RefreshToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var refreshedTokens = await response.Content.ReadFromJsonAsync<AuthTokensResponse>(TestContext.Current.CancellationToken);
        refreshedTokens.ShouldNotBeNull();
        refreshedTokens.RefreshToken.ShouldNotBe(loginTokens.RefreshToken);
        var previousToken = await FindTokenAsync(loginTokens.RefreshToken);
        var replacementToken = await FindTokenAsync(refreshedTokens.RefreshToken);
        previousToken.IsRevoked.ShouldBeTrue();
        previousToken.ReplacedByTokenId.ShouldBe(replacementToken.Id);
        replacementToken.FamilyId.ShouldBe(previousToken.FamilyId);
        replacementToken.IsRevoked.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Revoke_Whole_Token_Family_When_Already_Used_Refresh_Token_Is_Submitted_After_Grace_Period()
    {
        var user = await CreateUserAsync(Roles.Technician);
        using var client = Factory.CreateClient();
        var loginTokens = await client.LoginAsync(user);
        using var firstRefresh = await client.PostRefreshAsync(loginTokens.RefreshToken);
        var rotatedTokens = await firstRefresh.Content.ReadFromJsonAsync<AuthTokensResponse>(TestContext.Current.CancellationToken);
        rotatedTokens.ShouldNotBeNull();
        await MoveRotationBeforeGracePeriodAsync(loginTokens.RefreshToken);

        using var reuseResponse = await client.PostRefreshAsync(loginTokens.RefreshToken);
        using var rotatedTokenResponse = await client.PostRefreshAsync(rotatedTokens.RefreshToken);

        await reuseResponse.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, RefreshTokenErrors.Reused.Code);
        await rotatedTokenResponse.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, RefreshTokenErrors.Revoked.Code);
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        (await dbContext.RefreshTokens.AllAsync(token => token.RevokedAt != null, TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Return_Valid_Token_Pairs_When_Same_Refresh_Token_Is_Used_Twice_Within_Grace_Period()
    {
        var user = await CreateUserAsync(Roles.Technician);
        using var client = Factory.CreateClient();
        var loginTokens = await client.LoginAsync(user);

        using var firstResponse = await client.PostRefreshAsync(loginTokens.RefreshToken);
        using var secondResponse = await client.PostRefreshAsync(loginTokens.RefreshToken);

        var firstTokens = await ReadTokensAsync(firstResponse);
        var secondTokens = await ReadTokensAsync(secondResponse);
        firstTokens.RefreshToken.ShouldNotBe(secondTokens.RefreshToken);
        using var firstFollowUp = await client.PostRefreshAsync(firstTokens.RefreshToken);
        using var secondFollowUp = await client.PostRefreshAsync(secondTokens.RefreshToken);
        firstFollowUp.StatusCode.ShouldBe(HttpStatusCode.OK);
        secondFollowUp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_Return_Token_Pairs_For_Both_Requests_When_Same_Refresh_Token_Is_Used_Concurrently()
    {
        var user = await CreateUserAsync(Roles.Technician);
        using var client = Factory.CreateClient();
        var loginTokens = await client.LoginAsync(user);

        var responses = await Task.WhenAll(client.PostRefreshAsync(loginTokens.RefreshToken), client.PostRefreshAsync(loginTokens.RefreshToken));

        try
        {
            responses.Select(response => response.StatusCode).ShouldBe([HttpStatusCode.OK, HttpStatusCode.OK]);
            await using var scope = Factory.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
            (await dbContext.RefreshTokens.CountAsync(token => token.RevokedAt == null, TestContext.Current.CancellationToken)).ShouldBe(2);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task Should_Reject_Reuse_Within_Grace_Period_When_Session_Was_Logged_Out()
    {
        var user = await CreateUserAsync(Roles.Technician);
        using var client = Factory.CreateClient();
        var loginTokens = await client.LoginAsync(user);
        using var refreshResponse = await client.PostRefreshAsync(loginTokens.RefreshToken);
        var rotatedTokens = await ReadTokensAsync(refreshResponse);
        using var logoutResponse = await client.PostAsJsonAsync(new Uri("/api/v1/auth/logout", UriKind.Relative), new LogoutRequest(rotatedTokens.RefreshToken), TestContext.Current.CancellationToken);

        using var response = await client.PostRefreshAsync(loginTokens.RefreshToken);

        logoutResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, RefreshTokenErrors.Reused.Code);
    }

    [Fact]
    public async Task Should_Return_Unauthorized_Problem_When_Refresh_Token_Is_Unknown()
    {
        using var client = Factory.CreateClient();

        using var response = await client.PostRefreshAsync(RefreshTokenSecret.Generate());

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, RefreshTokenErrors.Invalid.Code);
    }

    [Fact]
    public async Task Should_Return_Unauthorized_Problem_When_Refresh_Token_Is_Expired()
    {
        var user = await CreateUserAsync(Roles.Technician);
        var secret = RefreshTokenSecret.Generate();
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
            dbContext.RefreshTokens.Add(RefreshToken.Issue(user.Id, RefreshTokenSecret.Hash(secret), DateTimeOffset.UtcNow.AddDays(-8), TimeSpan.FromDays(7)));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var client = Factory.CreateClient();
        using var response = await client.PostRefreshAsync(secret);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, RefreshTokenErrors.Expired.Code);
    }

    [Fact]
    public async Task Should_Reject_Refresh_When_Account_Is_Deactivated()
    {
        var user = await CreateUserAsync(Roles.Technician);
        using var client = Factory.CreateClient();
        var loginTokens = await client.LoginAsync(user);
        await DeactivateUserDirectlyAsync(user.Id);

        using var response = await client.PostRefreshAsync(loginTokens.RefreshToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, RefreshTokenErrors.Invalid.Code);
        (await FindTokenAsync(loginTokens.RefreshToken)).IsRevoked.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Keep_Session_Revoked_When_Deactivated_Account_Is_Activated_Again()
    {
        var user = await CreateUserAsync(Roles.Technician);
        using var client = Factory.CreateClient();
        var loginTokens = await client.LoginAsync(user);
        await DeactivateUserDirectlyAsync(user.Id);
        using var rejectedResponse = await client.PostRefreshAsync(loginTokens.RefreshToken);
        await ActivateUserDirectlyAsync(user.Id);

        using var response = await client.PostRefreshAsync(loginTokens.RefreshToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, RefreshTokenErrors.Revoked.Code);
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Refresh_Token_Is_Empty()
    {
        using var client = Factory.CreateClient();

        using var response = await client.PostRefreshAsync(string.Empty);

        await response.ShouldBeValidationProblemAsync("refreshToken");
    }

    [Fact]
    public async Task Should_Reject_Second_Rotation_When_Same_Token_Is_Rotated_Concurrently()
    {
        var user = await CreateUserAsync(Roles.Technician);
        using var client = Factory.CreateClient();
        var loginTokens = await client.LoginAsync(user);
        var tokenHash = RefreshTokenSecret.Hash(loginTokens.RefreshToken);
        await using var firstScope = Factory.Services.CreateAsyncScope();
        await using var secondScope = Factory.Services.CreateAsyncScope();
        var firstContext = firstScope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var firstCopy = await firstContext.RefreshTokens.SingleAsync(token => token.TokenHash == tokenHash, TestContext.Current.CancellationToken);
        var secondCopy = await secondContext.RefreshTokens.SingleAsync(token => token.TokenHash == tokenHash, TestContext.Current.CancellationToken);

        firstContext.RefreshTokens.Add(firstCopy.Rotate("first-replacement", DateTimeOffset.UtcNow, TimeSpan.FromDays(7)).Value);
        secondContext.RefreshTokens.Add(secondCopy.Rotate("second-replacement", DateTimeOffset.UtcNow, TimeSpan.FromDays(7)).Value);
        await firstContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<AuthTokensResponse> ReadTokensAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AuthTokensResponse>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    private async Task MoveRotationBeforeGracePeriodAsync(string secret)
    {
        var tokenHash = RefreshTokenSecret.Hash(secret);
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        await dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, DateTimeOffset.UtcNow.AddMinutes(-1)), TestContext.Current.CancellationToken);
    }

    private async Task ActivateUserDirectlyAsync(Guid userId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await userManager.FindByIdAsync(userId.ToString())).ShouldNotBeNull();
        user.Activate();
        (await userManager.UpdateAsync(user)).Succeeded.ShouldBeTrue();
    }

    private async Task<RefreshToken> FindTokenAsync(string secret)
    {
        var tokenHash = RefreshTokenSecret.Hash(secret);
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        return await dbContext.RefreshTokens.SingleAsync(token => token.TokenHash == tokenHash, TestContext.Current.CancellationToken);
    }
}
