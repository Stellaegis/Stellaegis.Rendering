using Stellaegis.Primitives;

namespace Stellaegis.Rendering;

/// <summary>
/// What <see cref="RenderingModule"/> can do beyond HTML, declared by the adapter that holds it. An application that
/// needs one turns it on as a parameter, and its startup fails when no adapter holds it:
/// <code>
/// services.AddStellaegisModuleParameter&lt;RenderingModule&gt;(RenderingCapabilities.Pdf);
/// </code>
/// </summary>
public static class RenderingCapabilities
{
    /// <summary>
    /// Rendering to PDF, held by a PDF engine such as <c>AddStellaegisPlaywrightRenderer</c>
    /// (Stellaegis.Rendering.Playwright). Without it, <see cref="ITemplateRenderer.RenderPdfAsync"/> fails and the
    /// rendering API maps no PDF route.
    /// </summary>
    public static ModuleCapability Pdf { get; } = new("pdf");
}
