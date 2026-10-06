using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests.Auth;

public sealed class EndpointAuthorizationTests(FixFlowApiFactory factory)
{
    private static readonly string[] AnonymousApiEndpoints =
    [
        "POST /api/v{version:apiVersion}/auth/login",
        "POST /api/v{version:apiVersion}/auth/refresh",
        "GET /api/v{version:apiVersion}/photos/{photoId:guid}",
    ];

    private static readonly string[] AnonymousInfrastructureRoutePrefixes = ["/health", "/api/v1/system/ready", "/openapi/", "/scalar"];

    [Fact]
    public void Should_Require_Authorization_When_Endpoint_Is_Not_Explicitly_Anonymous()
    {
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();

        var anonymousEndpoints = endpoints
            .Where(endpoint => !RequiresAuthorization(endpoint))
            .Select(Describe)
            .Where(description => !AnonymousInfrastructureRoutePrefixes.Any(prefix => description.Contains($" {prefix}", StringComparison.Ordinal)))
            .ToList();

        endpoints.Count.ShouldBeGreaterThan(AnonymousApiEndpoints.Length);
        anonymousEndpoints.ShouldBe(AnonymousApiEndpoints, ignoreOrder: true);
    }

    private static bool RequiresAuthorization(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0
        && endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null;

    private static string Describe(RouteEndpoint endpoint)
    {
        var httpMethods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? ["ANY"];
        return $"{string.Join(',', httpMethods)} /{endpoint.RoutePattern.RawText?.TrimStart('/')}";
    }
}
