using ErrorOr;

namespace FixFlow.Api.Domain.Photos;

public sealed class Photo
{
    public const int MaxSizeBytes = 1024 * 1024;

    public const int MaxDailyUploadsPerTechnician = 100;

    public static readonly TimeSpan DailyLimitWindow = TimeSpan.FromDays(1);

    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];

    private Photo()
    {
    }

    public Guid Id { get; private set; }

    public Guid TechnicianId { get; private set; }

    public byte[] Content { get; private set; } = [];

    public int SizeBytes { get; private set; }

    public DateTimeOffset UploadedAt { get; private set; }

    public static ErrorOr<Photo> Create(Guid id, Guid technicianId, byte[] content, DateTimeOffset now)
    {
        if (content.Length == 0)
        {
            return PhotoErrors.ContentRequired;
        }

        if (content.Length > MaxSizeBytes)
        {
            return PhotoErrors.ContentTooLarge;
        }

        if (!content.AsSpan().StartsWith(JpegSignature))
        {
            return PhotoErrors.ContentNotJpeg;
        }

        return new Photo
        {
            Id = id,
            TechnicianId = technicianId,
            Content = content,
            SizeBytes = content.Length,
            UploadedAt = now,
        };
    }

    public static ErrorOr<Success> EnsureWithinDailyLimit(int uploadsOfTechnicianWithinWindow) =>
        uploadsOfTechnicianWithinWindow < MaxDailyUploadsPerTechnician ? Result.Success : PhotoErrors.DailyLimitExceeded;

    public ErrorOr<Success> EnsureIsRetryOf(Guid technicianId) =>
        TechnicianId == technicianId ? Result.Success : PhotoErrors.IdConflict;
}
