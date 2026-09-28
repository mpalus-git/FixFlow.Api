using ErrorOr;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Common.Concurrency;

public static class EntityVersionExtensions
{
    public static uint VersionOf<TEntity>(this DbContext dbContext, TEntity entity)
        where TEntity : class =>
        dbContext.Entry(entity).Property<uint>(EntityTag.VersionProperty).CurrentValue;

    public static ErrorOr<Success> EnsureVersionMatches<TEntity>(this DbContext dbContext, TEntity entity, string ifMatch)
        where TEntity : class =>
        EntityTag.Matches(ifMatch, dbContext.VersionOf(entity)) ? Result.Success : PreconditionErrors.Failed;

    public static Versioned<TValue> Versioned<TEntity, TValue>(this DbContext dbContext, TEntity entity, TValue value)
        where TEntity : class =>
        new(value, dbContext.VersionOf(entity));
}
