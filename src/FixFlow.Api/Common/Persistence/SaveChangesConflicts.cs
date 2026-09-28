using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FixFlow.Api.Common.Persistence;

public static class SaveChangesConflicts
{
    public static readonly Error ConcurrentModification = Error.Conflict(
        "Persistence.ConcurrentModification",
        "The resource was changed by another request. Reload it and try again.");

    public static void RejectSaveIfChangedConcurrently(this DbContext dbContext, object entity)
    {
        var entry = dbContext.Entry(entity);
        if (entry.State == EntityState.Unchanged)
        {
            entry.State = EntityState.Modified;
        }
    }

    public static Task<ErrorOr<Success>> SaveChangesOrConflictAsync(this DbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.SaveChangesOrErrorAsync(ConcurrentModification, cancellationToken);

    public static Task<ErrorOr<Success>> SaveChangesOrConflictAsync(
        this DbContext dbContext,
        string constraintName,
        Error conflictError,
        CancellationToken cancellationToken) =>
        dbContext.SaveChangesOrErrorAsync(constraintName, conflictError, ConcurrentModification, cancellationToken);

    public static Task<ErrorOr<Success>> SaveChangesOrPreconditionFailedAsync(this DbContext dbContext, CancellationToken cancellationToken) =>
        dbContext.SaveChangesOrErrorAsync(PreconditionErrors.Failed, cancellationToken);

    public static Task<ErrorOr<Success>> SaveChangesOrPreconditionFailedAsync(
        this DbContext dbContext,
        string constraintName,
        Error conflictError,
        CancellationToken cancellationToken) =>
        dbContext.SaveChangesOrErrorAsync(constraintName, conflictError, PreconditionErrors.Failed, cancellationToken);

    private static async Task<ErrorOr<Success>> SaveChangesOrErrorAsync(this DbContext dbContext, Error concurrencyError, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return concurrencyError;
        }
    }

    private static async Task<ErrorOr<Success>> SaveChangesOrErrorAsync(
        this DbContext dbContext,
        string constraintName,
        Error conflictError,
        Error concurrencyError,
        CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesOrErrorAsync(concurrencyError, cancellationToken);
        }
        catch (DbUpdateException exception) when (IsViolationOf(exception, constraintName))
        {
            return conflictError;
        }
    }

    private static bool IsViolationOf(DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.CheckViolation } violation
        && violation.ConstraintName == constraintName;
}
