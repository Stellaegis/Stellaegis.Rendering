using System.Net.Http.Json;
using System.Text.Json;
using Stellaegis.Primitives;

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
        RenderingArguments.Check(template, data);

        using var response = await http.PostAsJsonAsync(
            "html",
            new RenderHtmlRequest(template, data),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<byte[]> RenderPdfAsync(
        string template,
        JsonElement data,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        RenderingArguments.Check(template, data);
        options?.Validate();

        using var response = await http.PostAsJsonAsync(
            "pdf",
            new RenderPdfRequest(template, data, options),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Reads the error the host answered, once, and raises the managed exceptions as they are raised in process,
    /// recognised by their code: a template at fault, PDF options at fault. Any other failure keeps the code and the
    /// message the host answered, when it answered problem details — the reason phrase otherwise.
    /// </summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (await response.ReadErrorAsync(ct) is not { } error)
            return;

        throw error.Code switch
        {
            TemplateException.Code => new TemplateException(error.Message),
            PdfOptionsException.Code => new PdfOptionsException(error.Message),
            _ => new HttpRequestException(
                $"The rendering API answered {(int)response.StatusCode}{(error.Code.Length > 0 ? " " + error.Code : "")}: {error.Message}",
                inner: null,
                response.StatusCode),
        };
    }
}
