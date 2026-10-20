using System.Text.RegularExpressions;
using Stellaegis.Primitives;

namespace Stellaegis.Rendering;

/// <summary>Paper format and margin settings for PDF generation. They travel with each request.</summary>
/// <remarks>
/// The settings describe the page, nothing else: how far the printing browser is confined belongs to the host
/// (<c>PlaywrightRendererOptions</c>). A setting the engine would refuse — an unknown format, a margin that is
/// not a CSS length — is refused before the engine, as <see cref="PdfOptionsException"/>, in both modes.
/// </remarks>
public sealed class PdfOptions
{
    /// <summary>The paper formats a PDF engine prints: ISO A0 to A6, and the North American Letter, Legal, Tabloid and Ledger.</summary>
    public static IReadOnlySet<string> Formats { get; } = new HashSet<string>(
        ["A0", "A1", "A2", "A3", "A4", "A5", "A6", "Letter", "Legal", "Tabloid", "Ledger"], StringComparer.OrdinalIgnoreCase);

    /// <summary>The paper format, one of <see cref="Formats"/>. Defaults to <c>A4</c>.</summary>
    public string Format { get; init; } = "A4";

    /// <summary>Whether to print CSS backgrounds. Defaults to <c>true</c>.</summary>
    public bool PrintBackground { get; init; } = true;

    /// <summary>Page margins. When <c>null</c>, the engine uses its default margins.</summary>
    public MarginOptions? Margin { get; init; }

    /// <summary>The first setting a PDF engine would refuse, or <see langword="null"/> when the options hold.</summary>
    public string? FindError()
    {
        if (Format is null || !Formats.Contains(Format))
            return $"'{Format}' is not a paper format: one of {string.Join(", ", Formats)}.";
        return Margin?.FindError();
    }

    /// <summary>Refuses options a PDF engine would refuse, before the engine.</summary>
    /// <exception cref="PdfOptionsException">The options do not hold: <see cref="FindError"/> says why.</exception>
    public void Validate()
    {
        if (FindError() is { } error)
            throw new PdfOptionsException(error);
    }
}

/// <summary>
/// Page margin values expressed as CSS length strings (e.g. "10mm", "1cm", "0.5in").
/// </summary>
public sealed partial class MarginOptions
{
    /// <summary>Top margin (e.g. "10mm").</summary>
    public string Top    { get; init; } = "10mm";
    /// <summary>Bottom margin (e.g. "10mm").</summary>
    public string Bottom { get; init; } = "10mm";
    /// <summary>Left margin (e.g. "10mm").</summary>
    public string Left   { get; init; } = "10mm";
    /// <summary>Right margin (e.g. "10mm").</summary>
    public string Right  { get; init; } = "10mm";

    /// <summary>The first margin that is not a CSS length — <c>10mm</c>, <c>1cm</c>, <c>0.5in</c>, <c>12px</c> —, or <see langword="null"/>.</summary>
    public string? FindError()
    {
        foreach (var (name, value) in new[] { ("Top", Top), ("Bottom", Bottom), ("Left", Left), ("Right", Right) })
        {
            if (value is null || !Length().IsMatch(value))
                return $"Margin.{name} '{value}' is not a CSS length: a number followed by px, in, cm or mm.";
        }
        return null;
    }

    [GeneratedRegex(@"^\d+(\.\d+)?(px|in|cm|mm)$", RegexOptions.CultureInvariant)]
    private static partial Regex Length();
}

/// <summary>
/// The PDF options are at fault: a format no engine prints, a margin that is not a CSS length. The caller fixes
/// the request. Thrown alike in both modes, before any engine runs, as a validation error of code <see cref="Code"/>:
/// the rendering API answers it with a <c>400</c> carrying that code, which the client raises again as this
/// exception.
/// </summary>
public sealed class PdfOptionsException(string message, Exception? innerException = null)
    : UserFacingException(Error.Validation(Code, message), innerException)
{
    /// <summary>The code of the error PDF options at fault carry.</summary>
    public const string Code = "PdfOptions.Invalid";
}
