using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stellaegis.Rendering.Scriban;

/// <summary>
/// Dependency injection helpers for Stellaegis.Rendering.Scriban.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Scriban <see cref="IHtmlRenderer"/>, as a singleton.
    /// </summary>
    public static IServiceCollection AddStellaegisScribanRenderer(this IServiceCollection services)
    {
        services.TryAddSingleton<IHtmlRenderer, ScribanHtmlRenderer>();
        return services;
    }
}
