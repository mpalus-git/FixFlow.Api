using FixFlow.Api.Features.Photos.GetPhoto;
using FixFlow.Api.Features.Photos.UploadPhoto;

namespace FixFlow.Api.Features.Photos;

public static class PhotosModule
{
    public const string JpegMediaType = "image/jpeg";

    public static IServiceCollection AddPhotosFeatures(this IServiceCollection services)
    {
        services.AddScoped<UploadPhotoHandler>();
        services.AddScoped<GetPhotoHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapPhotosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi("Photos")
            .MapGroup("/api/v{version:apiVersion}/photos")
            .HasApiVersion(1)
            .WithTags("Photos");

        group.MapUploadPhoto();
        group.MapGetPhoto();

        return app;
    }

    public static string PhotoPath(Guid photoId) => $"/api/v1/photos/{photoId}";
}
