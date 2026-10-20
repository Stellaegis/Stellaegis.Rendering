using Stellaegis.Primitives;

namespace Stellaegis.Rendering;

/// <summary>
/// The operations of the rendering module, declared on its module declaration and carried by the routes of
/// <c>Stellaegis.Rendering.AspNetCore</c>, one per route. The host keys its policies on their identity
/// (<c>RequireAuthorizationPerOperation</c>). Both are <see cref="OperationKind.Read"/>: a rendering changes no
/// data, whatever the HTTP verb that carries its request.
/// </summary>
/// <remarks>
/// HTML and PDF are two operations so that a host can protect them apart. Going through HTML to reach a PDF is
/// how the Playwright engine works, not a property of the API: another engine may render a template to PDF
/// directly. Both are declared whatever the engines registered: the PDF route is mapped only when an engine holds
/// the PDF capability, and an operation without a route is simply one the host never grants.
/// </remarks>
public static class RenderingOperations
{
    /// <summary>The target both operations act on: a template.</summary>
    public const string Target = "templates";

    /// <summary><c>POST rendering/html</c>: renders a template to HTML.</summary>
    public static readonly Operation RenderHtml = new(Target, "render_html", OperationKind.Read);

    /// <summary><c>POST rendering/pdf</c>: renders a template to PDF.</summary>
    public static readonly Operation RenderPdf = new(Target, "render_pdf", OperationKind.Read);
}
