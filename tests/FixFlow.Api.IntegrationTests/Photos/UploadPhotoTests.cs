using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FixFlow.Api.Common.Persistence;
using FixFlow.Api.Domain.Photos;
using FixFlow.Api.Domain.Users;
using FixFlow.Api.Features.Photos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests.Photos;

public sealed class UploadPhotoTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Store_Photo_With_Location_When_Technician_Uploads_Jpeg()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Technician);
        var photoId = Guid.CreateVersion7();
        var content = PhotoRequests.Jpeg();

        using var response = await client.PutPhotoAsync(photoId, content);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldBe(PhotoRequests.PhotoUri(photoId));
        var photo = await ReadPhotoAsync(response);
        photo.Id.ShouldBe(photoId);
        photo.SizeBytes.ShouldBe(content.Length);
        (await LoadStoredContentsAsync()).ShouldHaveSingleItem().ShouldBe(content);
    }

    [Fact]
    public async Task Should_Keep_First_Photo_When_Same_Technician_Uploads_Same_Identifier_Again()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Technician);
        var photoId = Guid.CreateVersion7();
        var firstContent = PhotoRequests.Jpeg(1000);
        using var firstResponse = await client.PutPhotoAsync(photoId, firstContent);

        using var secondResponse = await client.PutPhotoAsync(photoId, PhotoRequests.Jpeg(3000));

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadPhotoAsync(secondResponse)).SizeBytes.ShouldBe(firstContent.Length);
        (await LoadStoredContentsAsync()).ShouldHaveSingleItem().ShouldBe(firstContent);
    }

    [Fact]
    public async Task Should_Store_One_Photo_When_Photo_Is_Uploaded_Twice_Concurrently()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Technician);
        var photoId = Guid.CreateVersion7();
        var content = PhotoRequests.Jpeg();

        var responses = await Task.WhenAll(client.PutPhotoAsync(photoId, content), client.PutPhotoAsync(photoId, content));

        try
        {
            responses.Select(response => response.StatusCode).ShouldBe([HttpStatusCode.Created, HttpStatusCode.OK], ignoreOrder: true);
            (await LoadStoredContentsAsync()).ShouldHaveSingleItem();
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task Should_Return_Conflict_Problem_When_Identifier_Was_Used_By_Another_Technician()
    {
        using var firstClient = await CreateAuthenticatedClientAsync(Roles.Technician);
        using var secondClient = await CreateAuthenticatedClientAsync(Roles.Technician);
        var photoId = Guid.CreateVersion7();
        using var firstResponse = await firstClient.PutPhotoAsync(photoId, PhotoRequests.Jpeg());

        using var response = await secondClient.PutPhotoAsync(photoId, PhotoRequests.Jpeg());

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, PhotoErrors.IdConflict.Code);
    }

    [Fact]
    public async Task Should_Return_Unsupported_Media_Type_Problem_When_Content_Type_Is_Not_Jpeg()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Technician);

        using var response = await client.PutPhotoAsync(Guid.CreateVersion7(), PhotoRequests.Jpeg(), "image/png");

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await LoadStoredContentsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_Payload_Too_Large_Problem_When_Declared_Length_Exceeds_Max_Size()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Technician);

        using var response = await client.PutPhotoAsync(Guid.CreateVersion7(), PhotoRequests.Jpeg(Photo.MaxSizeBytes + 1));

        await response.ShouldBeProblemAsync(HttpStatusCode.RequestEntityTooLarge, PhotoErrors.ContentTooLarge.Code);
    }

    [Fact]
    public async Task Should_Return_Payload_Too_Large_Problem_When_Content_Without_Length_Exceeds_Max_Size()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Technician);
        using var body = new UnknownLengthContent(PhotoRequests.Jpeg(Photo.MaxSizeBytes + 1));
        body.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        using var response = await client.PutAsync(PhotoRequests.PhotoUri(Guid.CreateVersion7()), body, TestContext.Current.CancellationToken);

        await response.ShouldBeProblemAsync(HttpStatusCode.RequestEntityTooLarge, PhotoErrors.ContentTooLarge.Code);
        (await LoadStoredContentsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_Validation_Problem_When_Content_Is_Not_Jpeg()
    {
        using var client = await CreateAuthenticatedClientAsync(Roles.Technician);

        using var response = await client.PutPhotoAsync(Guid.CreateVersion7(), [0x89, 0x50, 0x4E, 0x47]);

        await response.ShouldBeValidationProblemAsync("content");
    }

    private static async Task<PhotoResponse> ReadPhotoAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<PhotoResponse>(TestContext.Current.CancellationToken)).ShouldNotBeNull();

    private async Task<List<byte[]>> LoadStoredContentsAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FixFlowDbContext>();
        return await dbContext.Photos.Select(photo => photo.Content).ToListAsync(TestContext.Current.CancellationToken);
    }
}
