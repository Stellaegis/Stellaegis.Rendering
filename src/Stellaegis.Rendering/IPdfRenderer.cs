namespace Stellaegis.Rendering;

/// <summary>
/// Renders an HTML string to PDF bytes — the port of a PDF engine.
/// </summary>
public interface IPdfRenderer
{
    /// <summary>
    /// Converts a complete HTML document to a PDF byte array.
    /// </summary>
    /// <param name="html">Full HTML document (including &lt;html&gt;, &lt;head&gt;, &lt;body&gt;).</param>
    /// <param name="options">Optional paper format and margin settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<byte[]> RenderAsync(
        string html,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default);
}
