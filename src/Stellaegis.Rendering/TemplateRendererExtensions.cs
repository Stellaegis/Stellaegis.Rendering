using System.Text.Json;

namespace Stellaegis.Rendering;

/// <summary>
/// Renders an object rather than a <see cref="JsonElement"/>: the object is serialized first, the
/// same way whatever adapter is registered.
/// </summary>
/// <remarks>
/// <para>
/// Property names become snake_case — <c>CustomerName</c> is <c>customer_name</c> in the template.
/// Dictionary keys are kept as they are. Values are what JSON carries: a date reaches the template
/// as an ISO 8601 string.
/// </para>
/// <para>
/// Serializing in process too is deliberate. Handing the object to the engine directly would keep
/// its <see cref="DateTime"/> values and the engine's own renaming, which a remote host never sees:
/// a template would work in process and break behind the API.
/// </para>
/// </remarks>
public static class TemplateRendererExtensions
{
    private static readonly JsonSerializerOptions DataSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>
    /// Serializes <paramref name="data"/> and renders <paramref name="template"/> with it, to HTML.
    /// </summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="template">Template source.</param>
    /// <param name="data">Data model, serialized with snake_case property names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task<string> RenderHtmlAsync<TData>(
        this ITemplateRenderer renderer,
        string template,
        TData data,
        CancellationToken cancellationToken = default) =>
        renderer.RenderHtmlAsync(template, ToJson(data), cancellationToken);

    /// <summary>
    /// Serializes <paramref name="data"/> and renders <paramref name="template"/> with it, to PDF.
    /// </summary>
    /// <param name="renderer">The renderer.</param>
    /// <param name="template">Template source, rendering a full HTML document.</param>
    /// <param name="data">Data model, serialized with snake_case property names.</param>
    /// <param name="options">Optional paper format and margin settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task<byte[]> RenderPdfAsync<TData>(
        this ITemplateRenderer renderer,
        string template,
        TData data,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default) =>
        renderer.RenderPdfAsync(template, ToJson(data), options, cancellationToken);

    private static JsonElement ToJson<TData>(TData data) =>
        JsonSerializer.SerializeToElement(data, DataSerializerOptions);
}
