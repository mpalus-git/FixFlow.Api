using FixFlow.Api.Features.Photos.UploadPhoto;

namespace FixFlow.Api.Features.Photos;

public static class PhotosModule
{
    public static IServiceCollection AddPhotosFeatures(this IServiceCollection services)
    {
        services.AddScoped<UploadPhotoHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapPhotosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.NewVersionedApi("Photos")
            .MapGroup("/api/v{version:apiVersion}/photos")
            .HasApiVersion(1)
            .WithTags("Photos");

        group.MapUploadPhoto();

        return app;
    }

    public static string PhotoPath(Guid photoId) => $"/api/v1/photos/{photoId}";
}
