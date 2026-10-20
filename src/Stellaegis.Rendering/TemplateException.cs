using Stellaegis.Primitives;

namespace Stellaegis.Rendering;

/// <summary>
/// The template is at fault: it does not parse, or it fails while rendering. Fixing it means
/// changing the template, not retrying.
/// </summary>
/// <remarks>
/// Thrown alike in both modes. It carries a validation error of code <see cref="Code"/>: the rendering API
/// answers it with a <c>400</c> carrying that code, which the HTTP client recognises and raises again as this
/// exception.
/// </remarks>
public sealed class TemplateException(string message, Exception? innerException = null)
    : UserFacingException(Error.Validation(Code, message), innerException)
{
    /// <summary>The code of the error a template at fault carries.</summary>
    public const string Code = "Template.Invalid";
}
