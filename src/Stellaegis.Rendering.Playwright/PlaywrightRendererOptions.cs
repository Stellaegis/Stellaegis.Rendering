namespace Stellaegis.Rendering.Playwright;

/// <summary>
/// How far Chromium is confined while it prints a PDF.
/// Bound from the <c>Stellaegis:Rendering:Playwright</c> configuration section by
/// <c>AddStellaegisPlaywrightRenderer</c>.
/// </summary>
/// <remarks>
/// These belong to the host and never to a request: on a remote host, the template comes from the
/// caller, and a request able to open the network would undo the confinement.
/// </remarks>
public sealed class PlaywrightRendererOptions
{
    /// <summary>Default configuration section name: <c>Stellaegis:Rendering:Playwright</c>.</summary>
    public const string SectionName = "Stellaegis:Rendering:Playwright";

    /// <summary>Whether the printed HTML may run JavaScript. Defaults to <c>false</c>.</summary>
    public bool JavaScriptEnabled { get; init; }

    /// <summary>
    /// Whether the printed HTML may load resources over the network — images, fonts, styles.
    /// Defaults to <c>false</c>: resources are embedded in the HTML.
    /// </summary>
    public bool NetworkEnabled { get; init; }

    /// <summary>
    /// Whether Chromium runs in its sandbox. Defaults to <c>true</c>. The sandbox cannot start in a
    /// container running as root, nor on Linux hosts where AppArmor restricts unprivileged user
    /// namespaces (Ubuntu 23.10 and later) unless they are allowed for Chromium: such a host either
    /// allows them or sets this to <c>false</c>.
    /// </summary>
    public bool Sandbox { get; init; } = true;
}
