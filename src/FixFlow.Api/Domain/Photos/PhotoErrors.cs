using ErrorOr;

namespace FixFlow.Api.Domain.Photos;

public static class PhotoErrors
{
    public const int ContentTooLargeType = StatusCodes.Status413PayloadTooLarge;
    public const int UnsupportedMediaTypeType = StatusCodes.Status415UnsupportedMediaType;

    public static readonly Error NotFound = Error.NotFound("Photo.NotFound", "Photo was not found.");

    public static readonly Error ContentRequired = Error.Validation("Content", "Photo content is required.");

    public static readonly Error ContentNotJpeg = Error.Validation("Content", "Photo content must be a JPEG image.");

    public static readonly Error ContentTooLarge = Error.Custom(
        ContentTooLargeType,
        "Photo.ContentTooLarge",
        $"Photo cannot be larger than {Photo.MaxSizeBytes} bytes.");

    public static readonly Error UnsupportedMediaType = Error.Custom(
        UnsupportedMediaTypeType,
        "Photo.UnsupportedMediaType",
        "Photo must be sent with the image/jpeg content type.");

    public static readonly Error IdConflict = Error.Conflict(
        "Photo.IdConflict",
        "A photo with this identifier was uploaded by another technician.");
}
