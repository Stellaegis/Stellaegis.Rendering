using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stellaegis.Rendering;

/// <summary>
/// Dependency injection helpers for Stellaegis.Rendering.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-process <see cref="ITemplateRenderer"/>, which renders with the engines
    /// registered in the same container: an <see cref="IHtmlRenderer"/>, required, and an
    /// <see cref="IPdfRenderer"/>, needed only to render PDF.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddStellaegisRendering()
    ///     .AddStellaegisScribanRenderer()
    ///     .AddStellaegisPlaywrightRenderer(builder.Configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddStellaegisRendering(this IServiceCollection services)
    {
        services.TryAddSingleton<ITemplateRenderer, TemplateRenderer>();
        return services;
    }
}
