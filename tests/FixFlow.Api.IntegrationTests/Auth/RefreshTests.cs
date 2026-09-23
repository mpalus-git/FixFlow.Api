using System.Net;
using System.Net.Http.Json;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Auth;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Auth;
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
    public async Task Should_Revoke_Whole_Token_Family_When_Already_Used_Refresh_Token_Is_Submitted()
    {
        var user = await CreateUserAsync(Roles.Technician);
        using var client = Factory.CreateClient();
        var loginTokens = await client.LoginAsync(user);
        using var firstRefresh = await client.PostRefreshAsync(loginTokens.RefreshToken);
        var rotatedTokens = await firstRefresh.Content.ReadFromJsonAsync<AuthTokensResponse>(TestContext.Current.CancellationToken);
        rotatedTokens.ShouldNotBeNull();

        using var reuseResponse = await client.PostRefreshAsync(loginTokens.RefreshToken);
        using var rotatedTokenResponse = await client.PostRefreshAsync(rotatedTokens.RefreshToken);

        await reuseResponse.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, RefreshTokenErrors.Reused.Code);
        await rotatedTokenResponse.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, RefreshTokenErrors.Revoked.Code);
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        (await dbContext.RefreshTokens.AllAsync(token => token.RevokedAt != null, TestContext.Current.CancellationToken)).ShouldBeTrue();
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
    public async Task Should_Return_Validation_Problem_When_Refresh_Token_Is_Empty()
    {
        using var client = Factory.CreateClient();

        using var response = await client.PostRefreshAsync(string.Empty);

        await response.ShouldBeValidationProblemAsync("RefreshToken");
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

    private async Task<RefreshToken> FindTokenAsync(string secret)
    {
        var tokenHash = RefreshTokenSecret.Hash(secret);
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        return await dbContext.RefreshTokens.SingleAsync(token => token.TokenHash == tokenHash, TestContext.Current.CancellationToken);
    }
}
