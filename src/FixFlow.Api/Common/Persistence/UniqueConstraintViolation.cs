using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FixFlow.Api.Common.Persistence;

public static class UniqueConstraintViolation
{
    public static async Task<ErrorOr<Success>> SaveChangesOrConflictAsync(
        this DbContext dbContext,
        string uniqueConstraintName,
        Error conflictError,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
        catch (DbUpdateException exception) when (IsViolationOf(exception, uniqueConstraintName))
        {
            return conflictError;
        }
    }

    private static bool IsViolationOf(DbUpdateException exception, string uniqueConstraintName) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } violation
        && violation.ConstraintName == uniqueConstraintName;
}
