using FixFlow.Api.Common.Pagination;
using FixFlow.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Parts.ListParts;

public sealed class ListPartsHandler(FixFlowDbContext dbContext)
{
    public Task<PagedResponse<PartResponse>> HandleAsync(ListPartsRequest request, CancellationToken cancellationToken)
    {
        var query = dbContext.Parts
            .AsNoTracking()
            .Where(part => part.ArchivedAt == null);

        var search = request.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = LikePattern.Contains(search);
            query = query.Where(part =>
                EF.Functions.ILike(part.Name, pattern, LikePattern.EscapeCharacter)
                || EF.Functions.ILike(part.CatalogNumber, pattern, LikePattern.EscapeCharacter));
        }

        return query
            .OrderBy(part => part.Name)
            .ThenBy(part => part.CatalogNumber)
            .ToPagedResponseAsync(request, PartResponse.FromDomain, cancellationToken);
    }
}
