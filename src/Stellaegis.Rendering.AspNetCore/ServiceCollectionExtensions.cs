using Microsoft.Extensions.DependencyInjection;
using Stellaegis.Primitives.AspNetCore;

namespace Stellaegis.Rendering.AspNetCore;

/// <summary>
/// Dependency injection helpers for the rendering endpoints.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers what the rendering endpoints need beyond the renderer: the startup check of the routes, which
    /// fails the host before it serves anything when a route is mapped without authorization. A host that maps
    /// <see cref="EndpointRouteBuilderExtensions.MapRenderingEndpoints"/> calls it, beside
    /// <c>AddStellaegisRendering</c>.
    /// </summary>
    /// <remarks>
    /// The module has no compartments, so no request context registers the check for it. Registering it twice is
    /// harmless.
    /// </remarks>
    public static IServiceCollection AddStellaegisRenderingEndpoints(this IServiceCollection services) =>
        services.AddStellaegisStartupCheck();
}
