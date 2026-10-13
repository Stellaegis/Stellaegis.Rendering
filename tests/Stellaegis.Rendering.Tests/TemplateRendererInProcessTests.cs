using Microsoft.Extensions.DependencyInjection;
using Stellaegis.Rendering.Scriban;
using static Stellaegis.Rendering.Tests.Rendering;

namespace Stellaegis.Rendering.Tests;

/// <summary>
/// What only the in-process adapter does: chain whatever engines are registered beside it.
/// </summary>
public sealed class TemplateRendererInProcessTests
{
    private static ServiceProvider HtmlOnly() =>
        new ServiceCollection().AddStellaegisRendering().AddStellaegisScribanRenderer().BuildServiceProvider();

    [Fact]
    public async Task Without_a_PDF_engine_HTML_renders_and_a_PDF_names_the_package_to_add()
    {
        await using var services = HtmlOnly();
        var renderer = services.GetRequiredService<ITemplateRenderer>();
        var ct = TestContext.Current.CancellationToken;

        Assert.Equal("ok", await renderer.RenderHtmlAsync("{{ v }}", Json("""{"v":"ok"}"""), ct));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => renderer.RenderPdfAsync("<p/>", Json("{}"), cancellationToken: ct));
        Assert.Contains("Stellaegis.Rendering.Playwright", error.Message);
    }

    [Fact]
    public async Task A_cancelled_rendering_throws_OperationCanceledException()
    {
        await using var services = HtmlOnly();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => services.GetRequiredService<ITemplateRenderer>()
            .RenderHtmlAsync("{{ for i in 1..10 }}{{ i }}{{ end }}", Json("{}"), cancelled.Token));
    }
}
