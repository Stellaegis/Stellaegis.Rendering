using System.Text.Json;

namespace Stellaegis.Rendering.Client;

/// <summary>
/// Client for the rendering API.
/// </summary>
public interface IRenderingClient
{
    /// <summary>
    /// Sends a Scriban <paramref name="template"/> and its <paramref name="data"/> to the rendering API
    /// and returns the rendered HTML string.
    /// </summary>
    Task<string> RenderHtmlAsync(
        string template,
        JsonElement data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a Scriban <paramref name="template"/> and its <paramref name="data"/> to the rendering API
    /// and returns the PDF as a byte array.
    /// </summary>
    Task<byte[]> RenderPdfAsync(
        string template,
        JsonElement data,
        CancellationToken cancellationToken = default);
}
