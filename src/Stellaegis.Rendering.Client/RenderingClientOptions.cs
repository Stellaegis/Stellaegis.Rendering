namespace Stellaegis.Rendering.Client;

/// <summary>
/// Configuration for the rendering API client.
/// Bind from the <c>Stellaegis:Rendering:Client</c> configuration section.
/// </summary>
public sealed class RenderingClientOptions
{
    /// <summary>Default configuration section name: <c>Stellaegis:Rendering:Client</c>.</summary>
    public const string SectionName = "Stellaegis:Rendering:Client";

    /// <summary>Base URL of the rendering API (e.g. <c>https://rendering.example.com/api/rendering/</c>).</summary>
    public required string BaseUrl { get; init; }

    /// <summary>HTTP timeout for rendering requests. Defaults to 60 seconds (PDF can be slow).</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);
}
