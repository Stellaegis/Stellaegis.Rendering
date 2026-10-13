using System.Net.Http.Json;
using System.Text.Json;

namespace Stellaegis.Rendering.Client;

internal sealed class RenderingClient : IRenderingClient
{
    private readonly HttpClient _http;

    public RenderingClient(HttpClient http) => _http = http;

    public async Task<string> RenderHtmlAsync(
        string template,
        JsonElement data,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "html",
            new RenderPayload(template, data),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<byte[]> RenderPdfAsync(
        string template,
        JsonElement data,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "pdf",
            new RenderPayload(template, data),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private sealed record RenderPayload(string Template, JsonElement Data);
}
