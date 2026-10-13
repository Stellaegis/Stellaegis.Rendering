using System.Text.Json;
using Scriban;
using Scriban.Runtime;

namespace Stellaegis.Rendering;

/// <summary>
/// Renders Scriban templates to HTML strings.
/// JSON properties are mapped to Scriban top-level variables using snake_case keys.
/// </summary>
/// <inheritdoc cref="IHtmlRenderer"/>
public sealed class ScribanHtmlRenderer : IHtmlRenderer
{
    /// <inheritdoc />
    public async Task<string> RenderAsync(
        string template,
        JsonElement data,
        CancellationToken cancellationToken = default)
    {
        var tpl = Template.Parse(template);
        if (tpl.HasErrors)
            throw new InvalidOperationException(
                $"Template parse error(s): {string.Join("; ", tpl.Messages)}");

        var context = new TemplateContext { StrictVariables = false };
        context.PushGlobal(ToScriptObject(data));

        return await tpl.RenderAsync(context);
    }

    private static ScriptObject ToScriptObject(JsonElement element)
    {
        var obj = new ScriptObject();
        foreach (var prop in element.EnumerateObject())
            obj[prop.Name] = ToValue(prop.Value);
        return obj;
    }

    private static object? ToValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => ToScriptObject(element),
        JsonValueKind.Array  => element.EnumerateArray().Select(ToValue).ToArray(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var l) ? (object)l : element.GetDouble(),
        JsonValueKind.True   => true,
        JsonValueKind.False  => false,
        _                    => null
    };
}
