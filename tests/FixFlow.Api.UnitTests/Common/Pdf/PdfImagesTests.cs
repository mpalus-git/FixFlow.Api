using FixFlow.Api.Common.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace FixFlow.Api.UnitTests.Common.Pdf;

public sealed class PdfImagesTests
{
    [Fact]
    public void Should_Accept_Image_When_Content_Is_Readable_Jpeg()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var jpeg = Document.Create(document => document.Page(page => page.Size(120, 40)))
            .GenerateImages(new ImageGenerationSettings { ImageFormat = ImageFormat.Jpeg, RasterDpi = 36 })
            .First();

        PdfImages.CanEmbed(jpeg).ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Image_When_Content_Only_Starts_Like_Jpeg()
    {
        byte[] content = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x00, 0x00];

        PdfImages.CanEmbed(content).ShouldBeFalse();
    }
}
