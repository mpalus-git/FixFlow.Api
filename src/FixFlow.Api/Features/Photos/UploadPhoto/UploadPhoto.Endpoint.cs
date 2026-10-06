using System.Security.Claims;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.Features.Photos.UploadPhoto;

public static class UploadPhotoEndpoint
{
    public static RouteGroupBuilder MapUploadPhoto(this RouteGroupBuilder group)
    {
        group.MapPut("/{photoId:guid}", async (Guid photoId, HttpRequest request, ClaimsPrincipal user, UploadPhotoHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(photoId, request, user, cancellationToken);
                return result.Match<IResult>(
                    uploaded => uploaded.WasAlreadyUploaded
                        ? TypedResults.Ok(uploaded.Photo)
                        : TypedResults.Created(PhotosModule.PhotoPath(uploaded.Photo.Id), uploaded.Photo),
                    errors => errors.ToProblem());
            })
            .WithName("UploadPhoto")
            .WithSummary("Upload a photo")
            .WithDescription("Stores a JPEG photo under an identifier chosen by the client, so the photo address /api/v1/photos/{photoId} is known before the upload and can be put in photoUrls of a service entry created offline. The body is the raw image/jpeg content of at most 1048576 bytes. Available to technicians only. When the identifier was already used by the same technician, the stored photo is kept and returned with status 200, so a queued upload can be safely sent again; the content is not compared. An identifier used by another technician is rejected with a conflict.")
            .RequireAuthorization(AuthorizationPolicies.TechnicianOnly)
            .Accepts<Stream>(UploadPhotoHandler.JpegMediaType)
            .Produces<PhotoResponse>()
            .Produces<PhotoResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

        return group;
    }
}
