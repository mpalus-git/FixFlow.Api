using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Parts;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Parts.GetPart;

public sealed class GetPartHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<Versioned<PartResponse>>> HandleAsync(Guid partId, CancellationToken cancellationToken)
    {
        var part = await dbContext.Parts.SingleOrDefaultAsync(part => part.Id == partId, cancellationToken);

        return part is null ? PartErrors.NotFound : dbContext.Versioned(part, PartResponse.FromDomain(part));
    }
}
