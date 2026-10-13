using System.Text.Json;

namespace Stellaegis.Documents.Client;

/// <summary>
/// Client for the Documents.Api rendering microservice.
/// </summary>
public interface IDocumentsClient
{
    /// <summary>
    /// Sends a Scriban <paramref name="template"/> and its <paramref name="data"/> to Documents.Api
    /// and returns the rendered HTML string.
    /// </summary>
    Task<string> RenderHtmlAsync(
        string template,
        JsonElement data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a Scriban <paramref name="template"/> and its <paramref name="data"/> to Documents.Api
    /// and returns the PDF as a byte array.
    /// </summary>
    Task<byte[]> RenderPdfAsync(
        string template,
        JsonElement data,
        CancellationToken cancellationToken = default);
}
