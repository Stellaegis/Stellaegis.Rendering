namespace Stellaegis.Documents.Client;

/// <summary>
/// Configuration for the Documents API client.
/// Bind from the <c>Stellaegis:Documents</c> configuration section.
/// </summary>
public sealed class DocumentsClientOptions
{
    /// <summary>Default configuration section name: <c>Stellaegis:Documents</c>.</summary>
    public const string SectionName = "Stellaegis:Documents";

    /// <summary>Base URL of the rendering host (e.g. <c>https://rendering.api.stellaegis.local/api/rendering/</c>).</summary>
    public required string BaseUrl { get; init; }

    /// <summary>OAuth2 scope required to call the rendering host. Defaults to <c>rendering</c>.</summary>
    public string Scope { get; init; } = "rendering";

    /// <summary>HTTP timeout for rendering requests. Defaults to 60 seconds (PDF can be slow).</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);
}
