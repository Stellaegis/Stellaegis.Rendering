using System.Text.Json;

namespace Stellaegis.Rendering;

/// <summary>
/// Renders a template against a JSON data model and returns the HTML output — the port of an HTML
/// template engine.
/// </summary>
public interface IHtmlRenderer
{
    /// <summary>
    /// Renders the given <paramref name="template"/> with <paramref name="data"/> as root context.
    /// </summary>
    /// <param name="template">Template source (inline string or pre-loaded content).</param>
    /// <param name="data">Data model, a JSON object: each property is a top-level variable of the
    /// template, under its JSON name unchanged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="TemplateException">The template does not parse, or fails while rendering.</exception>
    Task<string> RenderAsync(
        string template,
        JsonElement data,
        CancellationToken cancellationToken = default);
}
