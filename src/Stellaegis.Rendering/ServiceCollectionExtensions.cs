using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stellaegis.Primitives;
using Stellaegis.Primitives.DependencyInjection;

namespace Stellaegis.Rendering;

/// <summary>
/// Dependency injection helpers for Stellaegis.Rendering.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-process <see cref="ITemplateRenderer"/>, which renders with the engines
    /// registered in the same container: an <see cref="IHtmlRenderer"/>, required, and an
    /// <see cref="IPdfRenderer"/>, needed only to render PDF. Declares <see cref="RenderingModule"/> to the
    /// application, as a module without compartments: it holds no data, and serves any application frame.
    /// </summary>
    /// <remarks>
    /// A host calls it once: declaring the module twice fails the startup.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddStellaegisRendering()
    ///     .AddStellaegisScribanRenderer()
    ///     .AddStellaegisPlaywrightRenderer(builder.Configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddStellaegisRendering(this IServiceCollection services)
    {
        services.AddStellaegisModule(ModuleDeclaration.WithoutCompartments<RenderingModule>());
        services.TryAddSingleton<ITemplateRenderer, TemplateRenderer>();
        return services;
    }
}
