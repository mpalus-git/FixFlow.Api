using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Parts;
using FixFlow.Api.Domain.ServiceEntries;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.ServiceEntries;

public static class ServiceEntryResponses
{
    public static async Task<List<ServiceEntryResponse>> ToServiceEntryResponsesAsync(
        this FixFlowDbContext dbContext,
        IReadOnlyCollection<ServiceEntry> entries,
        IReadOnlyCollection<Part> loadedParts,
        CancellationToken cancellationToken)
    {
        var parts = loadedParts.ToDictionary(part => part.Id);
        var missingPartIds = entries
            .SelectMany(entry => entry.Parts)
            .Select(part => part.PartId)
            .Distinct()
            .Where(partId => !parts.ContainsKey(partId))
            .ToList();
        if (missingPartIds.Count > 0)
        {
            var missingParts = await dbContext.Parts
                .AsNoTracking()
                .Where(part => missingPartIds.Contains(part.Id))
                .ToListAsync(cancellationToken);
            foreach (var part in missingParts)
            {
                parts.Add(part.Id, part);
            }
        }

        var technicianIds = entries.Select(entry => entry.TechnicianId).Distinct().ToList();
        var technicianNames = await dbContext.Users
            .Where(technician => technicianIds.Contains(technician.Id))
            .ToDictionaryAsync(technician => technician.Id, technician => technician.FullName, cancellationToken);

        return entries.Select(entry => ServiceEntryResponse.FromDomain(entry, technicianNames[entry.TechnicianId], parts)).ToList();
    }
}
