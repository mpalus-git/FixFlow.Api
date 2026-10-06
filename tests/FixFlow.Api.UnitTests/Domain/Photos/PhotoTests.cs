using FixFlow.Api.Domain.Photos;

namespace FixFlow.Api.UnitTests.Domain.Photos;

public sealed class PhotoTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid PhotoId = Guid.NewGuid();
    private static readonly Guid TechnicianId = Guid.NewGuid();

    [Fact]
    public void Should_Create_Photo_With_Size_And_Upload_Time_When_Content_Is_Jpeg()
    {
        byte[] content = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

        var result = Photo.Create(PhotoId, TechnicianId, content, Now);

        result.IsError.ShouldBeFalse();
        result.Value.Id.ShouldBe(PhotoId);
        result.Value.TechnicianId.ShouldBe(TechnicianId);
        result.Value.Content.ShouldBe(content);
        result.Value.SizeBytes.ShouldBe(content.Length);
        result.Value.UploadedAt.ShouldBe(Now);
    }

    [Fact]
    public void Should_Accept_Photo_When_Content_Has_Max_Size()
    {
        var result = Photo.Create(PhotoId, TechnicianId, JpegOfSize(Photo.MaxSizeBytes), Now);

        result.IsError.ShouldBeFalse();
    }

    [Fact]
    public void Should_Reject_Photo_When_Content_Is_Empty()
    {
        var result = Photo.Create(PhotoId, TechnicianId, [], Now);

        result.FirstError.ShouldBe(PhotoErrors.ContentRequired);
    }

    [Fact]
    public void Should_Reject_Photo_When_Content_Exceeds_Max_Size()
    {
        var result = Photo.Create(PhotoId, TechnicianId, JpegOfSize(Photo.MaxSizeBytes + 1), Now);

        result.FirstError.ShouldBe(PhotoErrors.ContentTooLarge);
    }

    [Theory]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47 })]
    [InlineData(new byte[] { 0xFF, 0xD8 })]
    public void Should_Reject_Photo_When_Content_Is_Not_Jpeg(byte[] content)
    {
        var result = Photo.Create(PhotoId, TechnicianId, content, Now);

        result.FirstError.ShouldBe(PhotoErrors.ContentNotJpeg);
    }

    [Fact]
    public void Should_Accept_Retry_When_Photo_Was_Uploaded_By_Same_Technician()
    {
        var photo = Photo.Create(PhotoId, TechnicianId, JpegOfSize(10), Now).Value;

        photo.EnsureIsRetryOf(TechnicianId).IsError.ShouldBeFalse();
    }

    [Fact]
    public void Should_Reject_Retry_When_Photo_Was_Uploaded_By_Another_Technician()
    {
        var photo = Photo.Create(PhotoId, TechnicianId, JpegOfSize(10), Now).Value;

        photo.EnsureIsRetryOf(Guid.NewGuid()).FirstError.ShouldBe(PhotoErrors.IdConflict);
    }

    private static byte[] JpegOfSize(int size)
    {
        var content = new byte[size];
        content[0] = 0xFF;
        content[1] = 0xD8;
        content[2] = 0xFF;
        return content;
    }
}
