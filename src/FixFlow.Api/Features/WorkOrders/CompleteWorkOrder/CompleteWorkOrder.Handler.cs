using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Concurrency;
using FixFlow.Api.Common.Pdf;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Time;
using FixFlow.Api.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FixFlow.Api.Features.WorkOrders.CompleteWorkOrder;

public sealed class CompleteWorkOrderHandler(FixFlowDbContext dbContext, TimeProvider timeProvider, IOptions<ClientClockOptions> clientClockOptions)
{
    public async Task<ErrorOr<Versioned<WorkOrderResponse>>> HandleAsync(
        Guid workOrderId,
        CompleteWorkOrderRequest? request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.WorkOrders.VisibleTo(user).FindRowAsync(workOrderId, dbContext, cancellationToken);
        if (row is null)
        {
            return WorkOrderErrors.NotFound;
        }

        var workOrder = row.WorkOrder;

        var serviceEntries = await dbContext.ServiceEntries
            .Where(entry => entry.WorkOrderId == workOrderId)
            .GroupBy(entry => entry.WorkOrderId)
            .Select(entries => new { LastWorkFinishedAt = entries.Max(entry => entry.WorkFinishedAt) })
            .SingleOrDefaultAsync(cancellationToken);

        var completion = workOrder.Complete(
            hasServiceEntries: serviceEntries is not null,
            timeProvider.GetUtcNow(),
            request?.CompletedAt?.ToDatabasePrecision(),
            clientClockOptions.Value.MaxSkew,
            serviceEntries?.LastWorkFinishedAt,
            request?.ClientSignaturePhotoId is { } signaturePhotoId ? await LoadClientSignatureAsync(signaturePhotoId, cancellationToken) : null,
            user.GetUserId());
        if (completion.IsError)
        {
            return completion.Errors;
        }

        var saving = await dbContext.SaveChangesOrConflictAsync(cancellationToken);
        if (saving.IsError)
        {
            return saving.Errors;
        }

        return dbContext.VersionedResponse(row);
    }

    private async Task<ClientSignature> LoadClientSignatureAsync(Guid photoId, CancellationToken cancellationToken)
    {
        var photo = await dbContext.Photos
            .AsNoTracking()
            .Where(photo => photo.Id == photoId)
            .Select(photo => new { photo.TechnicianId, photo.Content })
            .SingleOrDefaultAsync(cancellationToken);

        return photo is null
            ? new ClientSignature(photoId, UploadedByTechnicianId: null, IsReadableImage: false)
            : new ClientSignature(photoId, photo.TechnicianId, PdfImages.CanEmbed(photo.Content));
    }
}
