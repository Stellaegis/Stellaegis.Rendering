using System.Net.Http.Json;
using System.Text.Json;

namespace Stellaegis.Rendering.Client;

/// <summary>
/// HTTP adapter of <see cref="ITemplateRenderer"/> — sends the template and its data to a remote
/// host exposing the rendering API, one call per rendering. Used when rendering is not hosted
/// in-process.
/// </summary>
internal sealed class HttpTemplateRenderer(HttpClient http) : ITemplateRenderer
{
    /// <inheritdoc />
    public async Task<string> RenderHtmlAsync(
        string template,
        JsonElement data,
        CancellationToken cancellationToken = default)
    {
        var response = await http.PostAsJsonAsync(
            "html",
            new RenderHtmlRequest(template, data),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<byte[]> RenderPdfAsync(
        string template,
        JsonElement data,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await http.PostAsJsonAsync(
            "pdf",
            new RenderPdfRequest(template, data, options),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
