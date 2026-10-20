using Stellaegis.Primitives;

namespace Stellaegis.Rendering;

/// <summary>
/// The identity of the rendering module, <c>rendering</c>: the routes of <c>Stellaegis.Rendering.AspNetCore</c>
/// belong to it.
/// </summary>
public sealed class RenderingModule : IStellaegisModule
{
    /// <inheritdoc />
    public static string Name => "rendering";
}
