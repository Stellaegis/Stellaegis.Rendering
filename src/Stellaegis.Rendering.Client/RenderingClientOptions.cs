using Stellaegis.Primitives.Hosting;

namespace Stellaegis.Rendering.Client;

/// <summary>
/// The options of the rendering API client: the base address of the host serving the rendering API, and the
/// timeout of each call — one minute unless set, since a PDF can be slow. Bound from the
/// <c>Stellaegis:Rendering:Client</c> configuration section, and validated when the host starts.
/// </summary>
public sealed class RenderingClientOptions : StellaegisClientOptions
{
    /// <summary>Default configuration section name: <c>Stellaegis:Rendering:Client</c>.</summary>
    public const string SectionName = "Stellaegis:Rendering:Client";
}
