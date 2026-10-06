using System.Net;
using FixFlow.Api.Domain.Users;

namespace FixFlow.Api.IntegrationTests.Photos;

public sealed class GetPhotoTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Jpeg_With_Cache_Headers_When_Photo_Is_Read_Without_Authentication()
    {
        var photoId = Guid.CreateVersion7();
        var content = await UploadPhotoAsync(photoId);
        using var anonymousClient = Factory.CreateClient();

        using var response = await anonymousClient.GetAsync(PhotoRequests.PhotoUri(photoId), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("image/jpeg");
        (await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).ShouldBe(content);
        response.Headers.ETag?.Tag.ShouldBe($"\"{photoId}\"");
        response.Headers.CacheControl.ShouldNotBeNull().Public.ShouldBeTrue();
        response.Headers.CacheControl.MaxAge.ShouldBe(TimeSpan.FromDays(365));
        response.Headers.CacheControl.Extensions.ShouldContain(extension => extension.Name == "immutable");
        response.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
    }

    [Fact]
    public async Task Should_Return_Not_Modified_When_If_None_Match_Has_Photo_ETag()
    {
        var photoId = Guid.CreateVersion7();
        await UploadPhotoAsync(photoId);
        using var anonymousClient = Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, PhotoRequests.PhotoUri(photoId));
        request.Headers.TryAddWithoutValidation("If-None-Match", $"\"{photoId}\"");

        using var response = await anonymousClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    private async Task<byte[]> UploadPhotoAsync(Guid photoId)
    {
        using var technicianClient = await CreateAuthenticatedClientAsync(Roles.Technician);
        var content = PhotoRequests.Jpeg();
        using var response = await technicianClient.PutPhotoAsync(photoId, content);
        response.EnsureSuccessStatusCode();
        return content;
    }
}
