using System.Text.Json;

namespace Stellaegis.Rendering;

/// <summary>
/// Request model for rendering a template to HTML via the rendering API.
/// Shared contract between the rendering API (server) and its callers (client).
/// </summary>
public sealed record RenderHtmlRequest(string Template, JsonElement Data);

/// <summary>
/// Request model for rendering a template to PDF via the rendering API.
/// Shared contract between the rendering API (server) and its callers (client).
/// </summary>
public sealed record RenderPdfRequest(string Template, JsonElement Data, PdfOptions? Options = null);
