namespace Stellaegis.Rendering;

/// <summary>Paper format and margin settings for PDF generation.</summary>
public sealed class PdfOptions
{
    /// <summary>
    /// CSS paper format: "A4", "A3", "Letter", "Legal", etc.
    /// Defaults to "A4".
    /// </summary>
    public string Format { get; init; } = "A4";

    /// <summary>Whether to print CSS backgrounds. Defaults to <c>true</c>.</summary>
    public bool PrintBackground { get; init; } = true;

    /// <summary>Page margins. When <c>null</c>, Playwright uses its default margins.</summary>
    public MarginOptions? Margin { get; init; }
}

/// <summary>
/// Page margin values expressed as CSS length strings (e.g. "10mm", "1cm", "0.5in").
/// </summary>
public sealed class MarginOptions
{
    /// <summary>Top margin (e.g. "10mm").</summary>
    public string Top    { get; init; } = "10mm";
    /// <summary>Bottom margin (e.g. "10mm").</summary>
    public string Bottom { get; init; } = "10mm";
    /// <summary>Left margin (e.g. "10mm").</summary>
    public string Left   { get; init; } = "10mm";
    /// <summary>Right margin (e.g. "10mm").</summary>
    public string Right  { get; init; } = "10mm";
}
