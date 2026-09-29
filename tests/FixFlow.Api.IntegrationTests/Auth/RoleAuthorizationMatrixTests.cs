using System.Net;
using System.Text.RegularExpressions;
using FixFlow.Api.Common.Auth;
using FixFlow.Api.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FixFlow.Api.IntegrationTests.Auth;

public sealed partial class RoleAuthorizationMatrixTests(FixFlowApiFactory factory) : IntegrationTestBase(factory)
{
    private static readonly Dictionary<string, string[]> AllowedRolesByPolicy = new()
    {
        [AuthorizationPolicies.AdminOnly] = [Roles.Admin],
        [AuthorizationPolicies.DispatcherOrAdmin] = [Roles.Dispatcher, Roles.Admin],
        [AuthorizationPolicies.TechnicianOnly] = [Roles.Technician],
    };

    private static readonly RoleRestrictedEndpoint[] RoleRestrictedEndpoints =
    [
        new("POST", "/api/v{version:apiVersion}/clients/", AuthorizationPolicies.DispatcherOrAdmin),
        new("PUT", "/api/v{version:apiVersion}/clients/{clientId:guid}", AuthorizationPolicies.DispatcherOrAdmin),
        new("POST", "/api/v{version:apiVersion}/clients/{clientId:guid}/archive", AuthorizationPolicies.DispatcherOrAdmin),
        new("POST", "/api/v{version:apiVersion}/devices/", AuthorizationPolicies.DispatcherOrAdmin),
        new("PUT", "/api/v{version:apiVersion}/devices/{deviceId:guid}", AuthorizationPolicies.DispatcherOrAdmin),
        new("POST", "/api/v{version:apiVersion}/devices/{deviceId:guid}/archive", AuthorizationPolicies.DispatcherOrAdmin),
        new("POST", "/api/v{version:apiVersion}/parts/", AuthorizationPolicies.DispatcherOrAdmin),
        new("PUT", "/api/v{version:apiVersion}/parts/{partId:guid}", AuthorizationPolicies.DispatcherOrAdmin),
        new("POST", "/api/v{version:apiVersion}/parts/{partId:guid}/archive", AuthorizationPolicies.DispatcherOrAdmin),
        new("POST", "/api/v{version:apiVersion}/parts/{partId:guid}/restock", AuthorizationPolicies.DispatcherOrAdmin),
        new("POST", "/api/v{version:apiVersion}/users/", AuthorizationPolicies.AdminOnly),
        new("GET", "/api/v{version:apiVersion}/users/", AuthorizationPolicies.DispatcherOrAdmin),
        new("POST", "/api/v{version:apiVersion}/users/{userId:guid}/deactivate", AuthorizationPolicies.AdminOnly),
        new("POST", "/api/v{version:apiVersion}/users/{userId:guid}/activate", AuthorizationPolicies.AdminOnly),
        new("POST", "/api/v{version:apiVersion}/users/{userId:guid}/password", AuthorizationPolicies.AdminOnly),
        new("POST", "/api/v{version:apiVersion}/work-orders/", AuthorizationPolicies.DispatcherOrAdmin),
        new("PUT", "/api/v{version:apiVersion}/work-orders/{workOrderId:guid}", AuthorizationPolicies.DispatcherOrAdmin),
        new("POST", "/api/v{version:apiVersion}/work-orders/{workOrderId:guid}/assign", AuthorizationPolicies.DispatcherOrAdmin),
        new("POST", "/api/v{version:apiVersion}/work-orders/{workOrderId:guid}/unassign", AuthorizationPolicies.DispatcherOrAdmin),
        new("POST", "/api/v{version:apiVersion}/work-orders/{workOrderId:guid}/start", AuthorizationPolicies.TechnicianOnly),
        new("POST", "/api/v{version:apiVersion}/work-orders/{workOrderId:guid}/invoice", AuthorizationPolicies.DispatcherOrAdmin),
        new("POST", "/api/v{version:apiVersion}/work-orders/{workOrderId:guid}/service-entries/", AuthorizationPolicies.TechnicianOnly),
        new("POST", "/api/v{version:apiVersion}/demo-data/reset", AuthorizationPolicies.AdminOnly),
    ];

    public static TheoryData<string, string, string> ForbiddenRequests { get; } = CreateForbiddenRequests();

    [Theory]
    [MemberData(nameof(ForbiddenRequests))]
    public async Task Should_Return_Forbidden_Problem_When_Role_Is_Not_Allowed_For_Endpoint(string method, string routePattern, string role)
    {
        using var client = await CreateAuthenticatedClientAsync(role);
        using var request = new HttpRequestMessage(new HttpMethod(method), ToRequestUri(routePattern));

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public void Should_List_Every_Endpoint_With_Role_Policy_When_Matrix_Is_Defined()
    {
        var endpointsWithRolePolicy = Factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => (Endpoint: endpoint, Policy: endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().LastOrDefault(data => data.Policy is not null)?.Policy))
            .Where(item => item.Policy is not null)
            .Select(item => new RoleRestrictedEndpoint(
                item.Endpoint.Metadata.GetRequiredMetadata<IHttpMethodMetadata>().HttpMethods.Single(),
                $"/{item.Endpoint.RoutePattern.RawText?.TrimStart('/')}",
                item.Policy!))
            .ToList();

        endpointsWithRolePolicy.ShouldBe(RoleRestrictedEndpoints, ignoreOrder: true);
    }

    [Fact]
    public async Task Should_Match_Policy_Roles_When_Matrix_Is_Defined()
    {
        var policyProvider = Factory.Services.GetRequiredService<IAuthorizationPolicyProvider>();

        foreach (var (policyName, allowedRoles) in AllowedRolesByPolicy)
        {
            var policy = (await policyProvider.GetPolicyAsync(policyName)).ShouldNotBeNull();
            var rolesRequirement = policy.Requirements.OfType<RolesAuthorizationRequirement>().ShouldHaveSingleItem();
            rolesRequirement.AllowedRoles.ShouldBe(allowedRoles, ignoreOrder: true);
        }
    }

    private static TheoryData<string, string, string> CreateForbiddenRequests()
    {
        var requests = new TheoryData<string, string, string>();
        foreach (var endpoint in RoleRestrictedEndpoints)
        {
            foreach (var role in Roles.All.Except(AllowedRolesByPolicy[endpoint.Policy]))
            {
                requests.Add(endpoint.Method, endpoint.RoutePattern, role);
            }
        }

        return requests;
    }

    private static Uri ToRequestUri(string routePattern)
    {
        var path = RouteParameter().Replace(
            routePattern.Replace("{version:apiVersion}", "1", StringComparison.Ordinal),
            _ => Guid.CreateVersion7().ToString());
        return new Uri(path, UriKind.Relative);
    }

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex RouteParameter();

    private sealed record RoleRestrictedEndpoint(string Method, string RoutePattern, string Policy);
}
