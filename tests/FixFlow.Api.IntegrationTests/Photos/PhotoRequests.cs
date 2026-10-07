using System.Net.Http.Headers;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace FixFlow.Api.IntegrationTests.Photos;

public static class PhotoRequests
{
    public static Uri PhotoUri(Guid photoId) => new($"/api/v1/photos/{photoId}", UriKind.Relative);

    public static byte[] Jpeg(int size = 2048)
    {
        var content = new byte[size];
        Random.Shared.NextBytes(content);
        content[0] = 0xFF;
        content[1] = 0xD8;
        content[2] = 0xFF;
        return content;
    }

    public static byte[] ReadableJpeg()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(document => document.Page(page =>
            {
                page.Size(240, 80);
                page.Content().AlignCenter().AlignMiddle().Text("Jan Nowak");
            }))
            .GenerateImages(new ImageGenerationSettings { ImageFormat = ImageFormat.Jpeg, RasterDpi = 72 })
            .First();
    }

    public static async Task<HttpResponseMessage> PutPhotoAsync(this HttpClient client, Guid photoId, byte[] content, string mediaType = "image/jpeg")
    {
        using var body = new ByteArrayContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        return await client.PutAsync(PhotoUri(photoId), body, TestContext.Current.CancellationToken);
    }
}
