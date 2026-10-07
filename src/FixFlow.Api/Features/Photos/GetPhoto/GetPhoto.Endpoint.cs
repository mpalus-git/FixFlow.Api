using FixFlow.Api.Common.Errors;
using Microsoft.Net.Http.Headers;

namespace FixFlow.Api.Features.Photos.GetPhoto;

public static class GetPhotoEndpoint
{
    public const string ImmutableCacheControl = "public, max-age=31536000, immutable";

    public static RouteGroupBuilder MapGetPhoto(this RouteGroupBuilder group)
    {
        group.MapGet("/{photoId:guid}", async (Guid photoId, HttpResponse response, GetPhotoHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(photoId, cancellationToken);
                return result.Match<IResult>(
                    photo =>
                    {
                        response.Headers.CacheControl = ImmutableCacheControl;
                        response.Headers.XContentTypeOptions = "nosniff";
                        return TypedResults.File(
                            photo.Content,
                            PhotosModule.JpegMediaType,
                            lastModified: photo.UploadedAt,
                            entityTag: new EntityTagHeaderValue($"\"{photo.Id}\""));
                    },
                    errors => errors.ToProblem());
            })
            .WithName("GetPhoto")
            .WithSummary("Get a photo")
            .WithDescription("Returns the stored JPEG photo. Available without authentication, so the photo address from photoUrls of a service entry can be used directly as an image source; anyone who knows the address can read the photo. A stored photo never changes, so the response can be cached for a year and carries the photo identifier as ETag; a request with a matching If-None-Match returns 304 without content.")
            .AllowAnonymous()
            .Produces<Stream>(StatusCodes.Status200OK, PhotosModule.JpegMediaType)
            .Produces(StatusCodes.Status304NotModified)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
