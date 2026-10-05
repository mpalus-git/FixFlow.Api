using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.ServiceEntries;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.ServiceEntries;

public static class ServiceEntryResponses
{
    public static async Task<List<ServiceEntryResponse>> ToServiceEntryResponsesAsync(
        this FixFlowDbContext dbContext,
        IReadOnlyCollection<ServiceEntry> entries,
        CancellationToken cancellationToken)
    {
        var partIds = entries.SelectMany(entry => entry.Parts).Select(part => part.PartId).Distinct().ToList();
        var parts = await dbContext.Parts
            .AsNoTracking()
            .Where(part => partIds.Contains(part.Id))
            .ToDictionaryAsync(part => part.Id, cancellationToken);

        var technicianIds = entries.Select(entry => entry.TechnicianId).Distinct().ToList();
        var technicianNames = await dbContext.Users
            .Where(technician => technicianIds.Contains(technician.Id))
            .ToDictionaryAsync(technician => technician.Id, technician => technician.FullName, cancellationToken);

        return entries.Select(entry => ServiceEntryResponse.FromDomain(entry, technicianNames[entry.TechnicianId], parts)).ToList();
    }
}
