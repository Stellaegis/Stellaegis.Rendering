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
    // An error body is quoted in the exception message; a host answering with a full HTML error page
    // would otherwise flood the logs.
    private const int MaxQuotedErrorLength = 1000;

    /// <inheritdoc />
    public async Task<string> RenderHtmlAsync(
        string template,
        JsonElement data,
        CancellationToken cancellationToken = default)
    {
        Guard.NotWhiteSpace(template);
        Guard.Satisfies(data, d => d.ValueKind == JsonValueKind.Object, "The data must be a JSON object.");

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
        Guard.NotWhiteSpace(template);
        Guard.Satisfies(data, d => d.ValueKind == JsonValueKind.Object, "The data must be a JSON object.");

        using var response = await http.PostAsJsonAsync(
            "pdf",
            new RenderPdfRequest(template, data, options),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Like <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/>, but keeps what the host said:
    /// a <c>400</c> names the faulty field, which the status code alone does not. A template at fault
    /// is raised as <see cref="TemplateException"/>, as it is in process: recognised by its code, or by
    /// <see cref="TemplateException.ProblemType"/> from a 4.0 host, which answers no code.
    /// </summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (await response.ReadErrorAsync(ct) is not { } error)
            return;

        if (error.Code == TemplateException.Code)
            throw new TemplateException(error.Message);

        var body = await response.Content.ReadAsStringAsync(ct);

        if (response.Content.Headers.ContentType?.MediaType == "application/problem+json"
            && TryReadProblem(body) is { Type: TemplateException.ProblemType } problem)
            throw new TemplateException(problem.Detail ?? "The template is at fault.");

        if (body.Length > MaxQuotedErrorLength)
            body = body[..MaxQuotedErrorLength] + "…";

        throw new HttpRequestException(
            $"The rendering API answered {(int)response.StatusCode} ({response.ReasonPhrase}): {body}",
            inner: null,
            response.StatusCode);
    }

    private static Problem? TryReadProblem(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<Problem>(body, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>The two fields of an RFC 9457 problem the client reads.</summary>
    private sealed record Problem(string? Type, string? Detail);
}
