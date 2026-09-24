using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Configurations;
using FixFlow.Api.Domain.Parts;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Parts.UpdatePart;

public sealed class UpdatePartHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<PartResponse>> HandleAsync(Guid partId, UpdatePartRequest request, CancellationToken cancellationToken)
    {
        var part = await dbContext.Parts.SingleOrDefaultAsync(part => part.Id == partId, cancellationToken);
        if (part is null)
        {
            return PartErrors.NotFound;
        }

        var update = part.Update(request.Name, request.CatalogNumber, request.UnitPrice);
        if (update.IsError)
        {
            return update.Errors;
        }

        var saving = await dbContext.SaveChangesOrConflictAsync(
            PartConfiguration.CatalogNumberIndexName,
            PartErrors.DuplicateCatalogNumber,
            cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        return PartResponse.FromDomain(part);
    }
}
