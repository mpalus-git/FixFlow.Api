using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Parts;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Parts.RestockPart;

public sealed class RestockPartHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<Versioned<PartResponse>>> HandleAsync(Guid partId, RestockPartRequest request, CancellationToken cancellationToken)
    {
        var part = await dbContext.Parts.SingleOrDefaultAsync(part => part.Id == partId, cancellationToken);
        if (part is null)
        {
            return PartErrors.NotFound;
        }

        part.Restock(request.Quantity);

        var saving = await dbContext.SaveChangesOrConflictAsync(cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        return dbContext.Versioned(part, PartResponse.FromDomain(part));
    }
}
