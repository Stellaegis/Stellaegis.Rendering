using System.Runtime.CompilerServices;
using System.Text.Json;
using Stellaegis.Primitives;

namespace Stellaegis.Rendering;

/// <summary>
/// What every adapter of <see cref="ITemplateRenderer"/> refuses before rendering, in one place: a blank template,
/// data that is not a JSON object. The client refuses them before sending, the engine before rendering, so that the
/// same call fails the same way in both modes.
/// </summary>
internal static class RenderingArguments
{
    /// <summary>Refuses a blank template or data that is not a JSON object, naming the parameter of the caller.</summary>
    /// <exception cref="ArgumentException">The template is blank, or the data is not a JSON object.</exception>
    public static void Check(
        string template,
        JsonElement data,
        [CallerArgumentExpression(nameof(template))] string? templateName = null,
        [CallerArgumentExpression(nameof(data))] string? dataName = null)
    {
        Guard.NotWhiteSpace(template, templateName);
        Guard.Satisfies(data, d => d.ValueKind == JsonValueKind.Object, "The data must be a JSON object.", dataName);
    }
}
