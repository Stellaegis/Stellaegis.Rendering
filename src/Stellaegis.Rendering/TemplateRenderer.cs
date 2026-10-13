using System.Text.Json;

namespace Stellaegis.Rendering;

/// <summary>
/// In-process adapter of <see cref="ITemplateRenderer"/>: chains the HTML engine and, for a PDF, the
/// PDF engine registered in the same container.
/// </summary>
/// <remarks>
/// The PDF engine is optional, so a consumer that renders only HTML does not carry a browser:
/// without one, <see cref="RenderPdfAsync"/> fails and nothing else does.
/// </remarks>
internal sealed class TemplateRenderer(IHtmlRenderer html, IPdfRenderer? pdf = null) : ITemplateRenderer
{
    /// <inheritdoc />
    public Task<string> RenderHtmlAsync(
        string template,
        JsonElement data,
        CancellationToken cancellationToken = default) =>
        html.RenderAsync(template, data, cancellationToken);

    /// <inheritdoc />
    public async Task<byte[]> RenderPdfAsync(
        string template,
        JsonElement data,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (pdf is null)
            throw new InvalidOperationException(
                $"No {nameof(IPdfRenderer)} is registered. Register a PDF engine, " +
                "such as AddStellaegisPlaywrightRenderer() from Stellaegis.Rendering.Playwright.");

        var rendered = await html.RenderAsync(template, data, cancellationToken);
        return await pdf.RenderAsync(rendered, options, cancellationToken);
    }
}
