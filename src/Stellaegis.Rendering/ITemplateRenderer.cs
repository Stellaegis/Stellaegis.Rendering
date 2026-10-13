using System.Text.Json;

namespace Stellaegis.Rendering;

/// <summary>
/// Renders a template and its data to HTML or to PDF — the port consumers depend on.
/// </summary>
/// <remarks>
/// The registered adapter decides where rendering happens: in process, over the engines registered
/// in the same container (<c>AddStellaegisRendering</c>), or on a remote host
/// (<c>AddStellaegisRenderingClient</c>, package <c>Stellaegis.Rendering.Client</c>). A template
/// renders the same in both, because the data is JSON in both.
/// </remarks>
public interface ITemplateRenderer
{
    /// <summary>
    /// Renders <paramref name="template"/> with <paramref name="data"/> as root context, to HTML.
    /// </summary>
    /// <param name="template">Template source.</param>
    /// <param name="data">Data model exposed to the template as top-level variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string> RenderHtmlAsync(
        string template,
        JsonElement data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders <paramref name="template"/> with <paramref name="data"/> as root context, to PDF, in a
    /// single call — a remote adapter sends the template and receives the PDF, never the HTML.
    /// </summary>
    /// <param name="template">Template source, rendering a full HTML document.</param>
    /// <param name="data">Data model exposed to the template as top-level variables.</param>
    /// <param name="options">Optional paper format and margin settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<byte[]> RenderPdfAsync(
        string template,
        JsonElement data,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default);
}
