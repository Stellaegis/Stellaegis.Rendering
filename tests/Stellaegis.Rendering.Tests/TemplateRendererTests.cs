using System.Text;
using static Stellaegis.Rendering.Tests.Rendering;

namespace Stellaegis.Rendering.Tests;

/// <summary>
/// The same cases against both adapters of <see cref="ITemplateRenderer"/>: a template renders the
/// same in process and behind the API, which is what the port promises.
/// </summary>
public sealed class TemplateRendererTests(ChromiumFixture chromium) : IClassFixture<ChromiumFixture>
{
    public static TheoryData<string> Modes => [InProcess, Remote];

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Json_properties_are_variables_under_their_own_names(string mode)
    {
        await using var session = await StartAsync(mode);

        var html = await session.Renderer.RenderHtmlAsync(
            "{{ customerName }}: {{ order.lines[0].qty }} × {{ order.lines[0].label }}",
            Json("""{"customerName":"Dupont","order":{"lines":[{"qty":2,"label":"chair"}]}}"""),
            TestContext.Current.CancellationToken);

        Assert.Equal("Dupont: 2 × chair", html);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task An_object_renders_with_snake_case_names_and_dates_as_ISO_strings(string mode)
    {
        await using var session = await StartAsync(mode);

        var html = await session.Renderer.RenderHtmlAsync(
            "{{ customer_name }} {{ issued_at }} {{ lines['Item A'] }}",
            new { CustomerName = "Dupont", IssuedAt = new DateTime(2026, 10, 13), Lines = new Dictionary<string, int> { ["Item A"] = 3 } },
            TestContext.Current.CancellationToken);

        Assert.Equal("Dupont 2026-10-13T00:00:00 3", html);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Values_are_inserted_unescaped_unless_the_template_escapes_them(string mode)
    {
        await using var session = await StartAsync(mode);

        var html = await session.Renderer.RenderHtmlAsync(
            "{{ v }} {{ v | html.escape }}",
            Json("""{"v":"<b>x</b>"}"""),
            TestContext.Current.CancellationToken);

        Assert.Equal("<b>x</b> &lt;b&gt;x&lt;/b&gt;", html);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Data_that_is_not_an_object_is_refused(string mode)
    {
        await using var session = await StartAsync(mode);

        var error = await Assert.ThrowsAsync<ArgumentException>(() => session.Renderer.RenderHtmlAsync(
            "{{ x }}", Json("[1]"), TestContext.Current.CancellationToken));
        Assert.Equal("data", error.ParamName);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task A_blank_template_is_refused(string mode)
    {
        await using var session = await StartAsync(mode);

        var error = await Assert.ThrowsAsync<ArgumentException>(() => session.Renderer.RenderHtmlAsync(
            " ", Json("{}"), TestContext.Current.CancellationToken));
        Assert.Equal("template", error.ParamName);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task A_template_at_fault_throws_TemplateException(string mode)
    {
        await using var session = await StartAsync(mode);
        var ct = TestContext.Current.CancellationToken;

        var parse = await Assert.ThrowsAsync<TemplateException>(
            () => session.Renderer.RenderHtmlAsync("{{ if }}", Json("{}"), ct));
        Assert.StartsWith("Template parse error(s):", parse.Message);

        var runtime = await Assert.ThrowsAsync<TemplateException>(
            () => session.Renderer.RenderHtmlAsync("{{ x.y.z() }}", Json("{}"), ct));
        Assert.StartsWith("Template runtime error:", runtime.Message);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task A_PDF_is_rendered_in_a_single_call_with_its_paper_format(string mode)
    {
        Assert.SkipWhen(chromium.Unavailable is not null, chromium.Unavailable ?? "");
        await using var session = await StartAsync(mode);
        var ct = TestContext.Current.CancellationToken;
        const string template = "<html><body><h1>{{ title }}</h1></body></html>";

        var a4 = await session.Renderer.RenderPdfAsync(template, Json("""{"title":"Invoice"}"""), new PdfOptions { Format = "A4" }, ct);
        var a5 = await session.Renderer.RenderPdfAsync(template, Json("""{"title":"Invoice"}"""), new PdfOptions { Format = "A5" }, ct);

        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(a4, 0, 5));
        Assert.NotEqual(MediaBox(a4), MediaBox(a5));
    }

    /// <summary>The page size Chromium writes in clear in the PDF: <c>/MediaBox [0 0 w h]</c>.</summary>
    private static string MediaBox(byte[] pdf)
    {
        var text  = Encoding.Latin1.GetString(pdf);
        var start = text.IndexOf("/MediaBox", StringComparison.Ordinal);
        Assert.True(start >= 0, "No /MediaBox in the PDF.");
        return text[start..text.IndexOf(']', start)];
    }
}
