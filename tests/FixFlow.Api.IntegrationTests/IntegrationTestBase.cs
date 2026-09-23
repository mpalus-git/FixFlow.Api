namespace FixFlow.Api.IntegrationTests;

public abstract class IntegrationTestBase(FixFlowApiFactory factory) : IAsyncLifetime
{
    protected FixFlowApiFactory Factory { get; } = factory;

    public async ValueTask InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
