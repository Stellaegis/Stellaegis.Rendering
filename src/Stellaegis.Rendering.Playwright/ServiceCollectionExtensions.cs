using Microsoft.Extensions.Configuration;
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
    /// browser, launched on the first PDF and confined as <see cref="PlaywrightRendererOptions"/> says.
    /// </summary>
    /// <remarks>
    /// Chromium must be installed on the host, by the script Playwright places in the application's
    /// output directory: <c>pwsh bin/Release/net10.0/playwright.ps1 install --with-deps chromium</c>.
    /// </remarks>
    /// <example>
    /// appsettings.json — the defaults:
    /// <code>
    /// "Stellaegis": {
    ///   "Rendering": {
    ///     "Playwright": { "JavaScriptEnabled": false, "NetworkEnabled": false, "Sandbox": true }
    ///   }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddStellaegisPlaywrightRenderer(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = PlaywrightRendererOptions.SectionName)
    {
        services.Configure<PlaywrightRendererOptions>(configuration.GetSection(sectionName));
        services.TryAddSingleton<IPdfRenderer, PlaywrightPdfRenderer>();
        return services;
    }
}
