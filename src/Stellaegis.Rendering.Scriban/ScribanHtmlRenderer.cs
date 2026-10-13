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
            throw new TemplateException(
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
        catch (ScriptRuntimeException e) when (e is not ScriptAbortException)
        {
            throw new TemplateException($"Template runtime error: {e.Message}", e);
        }
    }

    private static ScriptObject ToScriptObject(JsonElement element)
    {
        var obj = new ScriptObject();
        foreach (var prop in element.EnumerateObject())
            obj[prop.Name] = ToValue(prop.Value);
        return obj;
    }

    /// <summary>
    /// An integer stays an integer; any other number is a <see cref="decimal"/>, exact as written —
    /// an amount is computed and frozen upstream, and a template displays it, it does not round it
    /// again through binary floating point. Only a number beyond the range of <see cref="decimal"/>
    /// falls back to <see cref="double"/>.
    /// </summary>
    private static object ToNumber(JsonElement element) =>
        element.TryGetInt64(out var integer) ? integer
        : element.TryGetDecimal(out var exact) ? exact
        : element.GetDouble();

    private static object? ToValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => ToScriptObject(element),
        JsonValueKind.Array  => element.EnumerateArray().Select(ToValue).ToArray(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => ToNumber(element),
        JsonValueKind.True   => true,
        JsonValueKind.False  => false,
        _                    => null
    };
}
