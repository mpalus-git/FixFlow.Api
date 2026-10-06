using ErrorOr;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Photos;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Photos.GetPhoto;

public sealed class GetPhotoHandler(FixFlowDbContext dbContext)
{
    public async Task<ErrorOr<StoredPhoto>> HandleAsync(Guid photoId, CancellationToken cancellationToken)
    {
        var photo = await dbContext.Photos
            .AsNoTracking()
            .Where(photo => photo.Id == photoId)
            .Select(photo => new StoredPhoto(photo.Id, photo.Content, photo.UploadedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return photo is null ? PhotoErrors.NotFound : photo;
    }
}

public sealed record StoredPhoto(Guid Id, byte[] Content, DateTimeOffset UploadedAt);
