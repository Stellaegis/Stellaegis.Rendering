using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stellaegis.Rendering.Playwright;

/// <summary>
/// Dependency injection helpers for Stellaegis.Rendering.Playwright.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Playwright <see cref="IPdfRenderer"/>, as a singleton sharing one Chromium
    /// browser, launched on the first PDF.
    /// </summary>
    /// <remarks>
    /// Chromium must be installed on the host:
    /// <code>RUN dotnet tool install --global Microsoft.Playwright.CLI &amp;&amp; playwright install chromium --with-deps</code>
    /// </remarks>
    public static IServiceCollection AddStellaegisPlaywrightRenderer(this IServiceCollection services)
    {
        services.TryAddSingleton<IPdfRenderer, PlaywrightPdfRenderer>();
        return services;
    }
}
