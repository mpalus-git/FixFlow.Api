using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Parts;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Parts.ArchivePart;

public sealed class ArchivePartHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<Success>> HandleAsync(Guid partId, CancellationToken cancellationToken)
    {
        var part = await dbContext.Parts.SingleOrDefaultAsync(part => part.Id == partId, cancellationToken);
        if (part is null)
        {
            return PartErrors.NotFound;
        }

        part.Archive(timeProvider.GetUtcNow());

        return await dbContext.SaveChangesOrConflictAsync(cancellationToken);
    }
}
