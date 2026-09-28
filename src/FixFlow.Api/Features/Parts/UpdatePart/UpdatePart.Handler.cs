using ErrorOr;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Configurations;
using FixFlow.Api.Domain.Parts;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Parts.UpdatePart;

public sealed class UpdatePartHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<Versioned<PartResponse>>> HandleAsync(
        Guid partId,
        UpdatePartRequest request,
        string ifMatch,
        CancellationToken cancellationToken)
    {
        var part = await dbContext.Parts.SingleOrDefaultAsync(part => part.Id == partId, cancellationToken);
        if (part is null)
        {
            return PartErrors.NotFound;
        }

        var precondition = dbContext.EnsureVersionMatches(part, ifMatch);
        if (precondition.IsError)
        {
            return precondition.Errors;
        }

        var update = part.Update(request.Name, request.CatalogNumber, request.UnitPrice);
        if (update.IsError)
        {
            return update.Errors;
        }

        var saving = await dbContext.SaveChangesOrPreconditionFailedAsync(
            PartConfiguration.CatalogNumberIndexName,
            PartErrors.DuplicateCatalogNumber,
            cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        return dbContext.Versioned(part, PartResponse.FromDomain(part));
    }
}
