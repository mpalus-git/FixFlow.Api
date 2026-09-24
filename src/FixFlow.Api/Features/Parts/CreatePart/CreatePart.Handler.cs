using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Configurations;
using FixFlow.Api.Domain.Parts;

namespace FixFlow.Api.Features.Parts.CreatePart;

public sealed class CreatePartHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<ErrorOr<PartResponse>> HandleAsync(CreatePartRequest request, CancellationToken cancellationToken)
    {
        var part = Part.Create(request.Name, request.CatalogNumber, request.StockQuantity, request.UnitPrice, timeProvider.GetUtcNow());

        dbContext.Parts.Add(part);
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
