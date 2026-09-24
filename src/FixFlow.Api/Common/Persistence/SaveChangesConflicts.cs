using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FixFlow.Api.Common.Persistence;

public static class SaveChangesConflicts
{
    public static readonly Error ConcurrentModification = Error.Conflict(
        "Persistence.ConcurrentModification",
        "The resource was changed by another request. Reload it and try again.");

    public static async Task<ErrorOr<Success>> SaveChangesOrConflictAsync(this DbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return ConcurrentModification;
        }
    }

    public static async Task<ErrorOr<Success>> SaveChangesOrConflictAsync(
        this DbContext dbContext,
        string constraintName,
        Error conflictError,
        CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesOrConflictAsync(cancellationToken);
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
