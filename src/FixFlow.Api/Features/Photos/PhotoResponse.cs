using System.ComponentModel;
using FixFlow.Api.Domain.Photos;

namespace FixFlow.Api.Features.Photos;

public sealed record PhotoResponse(
    [property: Description("Identifier given by the client when uploading.")] Guid Id,
    [property: Description("Size of the photo in bytes.")] int SizeBytes,
    [property: Description("Time of the first successful upload.")] DateTimeOffset UploadedAt)
{
    public static PhotoResponse FromPhoto(Photo photo) => new(photo.Id, photo.SizeBytes, photo.UploadedAt);
}
