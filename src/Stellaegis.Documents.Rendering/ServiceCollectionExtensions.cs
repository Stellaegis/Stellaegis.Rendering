using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stellaegis.Documents.Rendering.Pdf;

namespace Stellaegis.Documents.Rendering;

/// <summary>
/// Dependency injection helpers for Stellaegis.Documents.Rendering.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IHtmlRenderer"/> (Scriban) and <see cref="IPdfRenderer"/> (Playwright/Chromium).
    /// Both are registered as singletons; the Chromium browser is lazily initialised on the first PDF request.
    /// </summary>
    /// <remarks>
    /// Docker setup: ensure Chromium is installed in the image.
    /// <code>RUN dotnet tool install --global Microsoft.Playwright.CLI &amp;&amp; playwright install chromium --with-deps</code>
    /// </remarks>
    public static IServiceCollection AddStellaegisDocumentRendering(this IServiceCollection services)
    {
        services.TryAddSingleton<IHtmlRenderer, ScribanHtmlRenderer>();
        services.TryAddSingleton<IPdfRenderer, PlaywrightPdfRenderer>();
        return services;
    }
}
