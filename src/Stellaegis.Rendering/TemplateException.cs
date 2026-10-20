using Stellaegis.Primitives;

namespace Stellaegis.Rendering;

/// <summary>
/// The template is at fault: it does not parse, or it fails while rendering. Fixing it means
/// changing the template, not retrying.
/// </summary>
/// <remarks>
/// Thrown alike in both modes. It carries a validation error of code <see cref="Code"/>: the rendering API
/// answers it with a <c>400</c>, which the HTTP client recognises by that code and raises again as this exception.
/// </remarks>
public sealed class TemplateException(string message, Exception? innerException = null)
    : UserFacingException(Error.Validation(Code, message), innerException)
{
    /// <summary>The code of the error a template at fault carries.</summary>
    public const string Code = "Template.Invalid";

    /// <summary>The <c>type</c> of the problem the rendering API answers for a template at fault.</summary>
    /// <remarks>
    /// The only mark a 4.0 host or client knows, so the API still answers it and the client still recognises it.
    /// Recognise <see cref="Code"/> instead: this type goes in the next major version.
    /// </remarks>
    public const string ProblemType = "urn:stellaegis:rendering:template-error";
}
