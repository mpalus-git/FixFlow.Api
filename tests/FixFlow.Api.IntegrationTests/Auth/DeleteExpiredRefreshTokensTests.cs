using FixFlow.Api.Common.Jobs;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Time;
using FixFlow.Api.Domain.Auth;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Auth.DeleteExpiredRefreshTokens;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Quartz;

namespace FixFlow.Api.IntegrationTests.Auth;

public sealed class DeleteExpiredRefreshTokensTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly DateTimeOffset FamilyStart = new(2026, 1, 5, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(7);

    [Fact]
    public async Task Should_Delete_All_Tokens_Of_Family_When_Newest_Token_Expired_Before_Retention_Period()
    {
        var familyId = await IssueFamilyAsync(rotations: 1, rotationInterval: TimeSpan.FromDays(1));
        var newestExpiry = FamilyStart.AddDays(1) + TokenLifetime;

        var deletedCount = await DeleteExpiredRefreshTokensAtAsync(newestExpiry + DeleteExpiredRefreshTokensHandler.RetentionAfterFamilyExpiry + TimeSpan.FromHours(1));

        deletedCount.ShouldBe(2);
        (await CountFamilyTokensAsync(familyId)).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Keep_Old_Expired_Tokens_When_Family_Still_Has_Recent_Token()
    {
        var familyId = await IssueFamilyAsync(rotations: 7, rotationInterval: TimeSpan.FromDays(6));
        var firstTokenExpiry = FamilyStart + TokenLifetime;

        var deletedCount = await DeleteExpiredRefreshTokensAtAsync(firstTokenExpiry + DeleteExpiredRefreshTokensHandler.RetentionAfterFamilyExpiry + TimeSpan.FromDays(1));

        deletedCount.ShouldBe(0);
        (await CountFamilyTokensAsync(familyId)).ShouldBe(8);
    }

    [Fact]
    public async Task Should_Keep_Family_When_Its_Newest_Token_Expired_Within_Retention_Period()
    {
        var familyId = await IssueFamilyAsync(rotations: 0, rotationInterval: TimeSpan.Zero);

        var deletedCount = await DeleteExpiredRefreshTokensAtAsync(FamilyStart + TokenLifetime + DeleteExpiredRefreshTokensHandler.RetentionAfterFamilyExpiry - TimeSpan.FromHours(1));

        deletedCount.ShouldBe(0);
        (await CountFamilyTokensAsync(familyId)).ShouldBe(1);
    }

    [Fact]
    public async Task Should_Schedule_Daily_Cleanup_At_Three_Warsaw_Time_When_Jobs_Are_Enabled()
    {
        await using var factoryWithJobs = Factory.WithWebHostBuilder(builder => builder.UseSetting(JobsExtensions.EnabledSettingKey, "true"));
        var scheduler = await factoryWithJobs.Services.GetRequiredService<ISchedulerFactory>().GetScheduler(TestContext.Current.CancellationToken);

        var triggers = await scheduler.GetTriggersOfJob(DeleteExpiredRefreshTokensJob.Key, TestContext.Current.CancellationToken);

        var trigger = triggers.ShouldHaveSingleItem().ShouldBeAssignableTo<ICronTrigger>().ShouldNotBeNull();
        trigger.CronExpressionString.ShouldBe(DeleteExpiredRefreshTokensJob.DailyCronExpression);
        trigger.TimeZone.Id.ShouldBe(BusinessTime.Zone.Id);
    }

    private async Task<Guid> IssueFamilyAsync(int rotations, TimeSpan rotationInterval)
    {
        var user = await CreateUserAsync(Roles.Technician);
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        var token = RefreshToken.Issue(user.Id, Guid.NewGuid().ToString("N"), FamilyStart, TokenLifetime);
        dbContext.RefreshTokens.Add(token);
        for (var rotation = 1; rotation <= rotations; rotation++)
        {
            token = token.Rotate(Guid.NewGuid().ToString("N"), FamilyStart + (rotationInterval * rotation), TokenLifetime).Value;
            dbContext.RefreshTokens.Add(token);
        }

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        return token.FamilyId;
    }

    private async Task<int> CountFamilyTokensAsync(Guid familyId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        return await dbContext.RefreshTokens.CountAsync(token => token.FamilyId == familyId, TestContext.Current.CancellationToken);
    }

    private async Task<int> DeleteExpiredRefreshTokensAtAsync(DateTimeOffset now)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var handler = new DeleteExpiredRefreshTokensHandler(
            scope.ServiceProvider.GetRequiredService<FixFlowDbContext>(),
            new FakeTimeProvider(now),
            NullLogger<DeleteExpiredRefreshTokensHandler>.Instance);
        return await handler.HandleAsync(TestContext.Current.CancellationToken);
    }
}
