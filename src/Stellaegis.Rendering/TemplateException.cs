namespace Stellaegis.Rendering;

/// <summary>
/// The template is at fault: it does not parse, or it fails while rendering. Fixing it means
/// changing the template, not retrying.
/// </summary>
/// <remarks>
/// Thrown alike in both modes. The rendering API answers it with a <c>400</c> problem of type
/// <see cref="ProblemType"/>, which the HTTP client raises again as this exception.
/// </remarks>
public sealed class TemplateException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    /// <summary>The <c>type</c> of the problem the rendering API answers for a template at fault.</summary>
    public const string ProblemType = "urn:stellaegis:rendering:template-error";
}
