using System.Security.Claims;
using ErrorOr;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Common.Persistence.Configurations;
using FixFlow.Api.Domain.Photos;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Features.Photos.UploadPhoto;

public sealed class UploadPhotoHandler(FixFlowDbContext dbContext, TimeProvider timeProvider)
{
    private const int ReadChunkSize = 64 * 1024;

    public async Task<ErrorOr<UploadedPhoto>> HandleAsync(Guid photoId, HttpRequest request, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var content = await ReadContentAsync(request.Body, request.ContentLength, cancellationToken);
        if (content.IsError)
        {
            return content.Errors;
        }

        var technicianId = user.GetUserId();
        var now = timeProvider.GetUtcNow();
        var creation = Photo.Create(photoId, technicianId, content.Value, now);
        if (creation.IsError)
        {
            return creation.Errors;
        }

        if (await FindRetriedPhotoAsync(photoId, technicianId, cancellationToken) is { } retriedPhoto)
        {
            return retriedPhoto;
        }

        var windowStart = now - Photo.DailyLimitWindow;
        var uploadsWithinWindow = await dbContext.Photos.CountAsync(
            photo => photo.TechnicianId == technicianId && photo.UploadedAt > windowStart,
            cancellationToken);
        var dailyLimit = Photo.EnsureWithinDailyLimit(uploadsWithinWindow);
        if (dailyLimit.IsError)
        {
            return dailyLimit.Errors;
        }

        dbContext.Photos.Add(creation.Value);
        var saving = await dbContext.SaveChangesOrConflictAsync(PhotoConfiguration.PrimaryKeyName, PhotoErrors.IdConflict, cancellationToken);
        if (saving.IsError)
        {
            dbContext.ChangeTracker.Clear();
            return await FindRetriedPhotoAsync(photoId, technicianId, cancellationToken) ?? saving.Errors;
        }

        return new UploadedPhoto(PhotoResponse.FromPhoto(creation.Value), WasAlreadyUploaded: false);
    }

    private static async Task<ErrorOr<byte[]>> ReadContentAsync(Stream body, long? contentLength, CancellationToken cancellationToken)
    {
        if (contentLength > Photo.MaxSizeBytes)
        {
            return PhotoErrors.ContentTooLarge;
        }

        using var content = new MemoryStream();
        var chunk = new byte[ReadChunkSize];
        int readBytes;
        while ((readBytes = await body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (content.Length + readBytes > Photo.MaxSizeBytes)
            {
                return PhotoErrors.ContentTooLarge;
            }

            content.Write(chunk, 0, readBytes);
        }

        return content.ToArray();
    }

    private async Task<ErrorOr<UploadedPhoto>?> FindRetriedPhotoAsync(Guid photoId, Guid technicianId, CancellationToken cancellationToken)
    {
        var existingPhoto = await dbContext.Photos
            .AsNoTracking()
            .SingleOrDefaultAsync(photo => photo.Id == photoId, cancellationToken);
        if (existingPhoto is null)
        {
            return null;
        }

        var retry = existingPhoto.EnsureIsRetryOf(technicianId);
        if (retry.IsError)
        {
            return retry.Errors;
        }

        return new UploadedPhoto(PhotoResponse.FromPhoto(existingPhoto), WasAlreadyUploaded: true);
    }
}

public sealed record UploadedPhoto(PhotoResponse Photo, bool WasAlreadyUploaded);
