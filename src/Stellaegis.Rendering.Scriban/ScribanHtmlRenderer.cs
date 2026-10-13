using System.Text.Json;
using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;
using Stellaegis.Primitives;

namespace Stellaegis.Rendering.Scriban;

/// <summary>
/// Renders Scriban templates to HTML strings.
/// </summary>
/// <remarks>
/// <para>
/// Each property of the JSON data becomes a top-level variable under its JSON name, unchanged:
/// <c>{"customer_name": …}</c> is <c>customer_name</c>, <c>{"customerName": …}</c> is
/// <c>customerName</c>.
/// </para>
/// <para>
/// Values are inserted as they are, <b>not HTML-escaped</b>: a value holding markup becomes markup.
/// A template escapes a value with <c>{{ value | html.escape }}</c>.
/// </para>
/// </remarks>
internal sealed class ScribanHtmlRenderer : IHtmlRenderer
{
    /// <inheritdoc />
    public async Task<string> RenderAsync(
        string template,
        JsonElement data,
        CancellationToken cancellationToken = default)
    {
        Guard.NotWhiteSpace(template);
        Guard.Satisfies(data, d => d.ValueKind == JsonValueKind.Object, "The data must be a JSON object.");

        var tpl = Template.Parse(template);
        if (tpl.HasErrors)
            throw new InvalidOperationException(
                $"Template parse error(s): {string.Join("; ", tpl.Messages)}");

        var context = new TemplateContext
        {
            StrictVariables   = false,
            CancellationToken = cancellationToken
        };
        context.PushGlobal(ToScriptObject(data));

        try
        {
            return await tpl.RenderAsync(context);
        }
        catch (ScriptAbortException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
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
