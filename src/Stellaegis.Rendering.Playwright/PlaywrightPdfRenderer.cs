using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Stellaegis.Primitives;

namespace Stellaegis.Rendering.Playwright;

/// <summary>
/// Renders HTML to PDF using a shared Playwright Chromium browser instance.
/// Registered as a singleton; the browser is lazily initialised on first use.
/// </summary>
/// <remarks>
/// <para>
/// Chromium runs the HTML it prints, on the host. By default it is confined
/// (<see cref="PlaywrightRendererOptions"/>): no JavaScript, no network, sandboxed. Otherwise a
/// template could have the host call an internal address and print the answer into the PDF.
/// Images, fonts and styles are embedded in the HTML — inline, or as <c>data:</c> URLs.
/// </para>
/// <para><b>Docker setup:</b> the Playwright Chromium driver must be installed in the image:</para>
/// <code>RUN dotnet tool install --global Microsoft.Playwright.CLI &amp;&amp; playwright install chromium --with-deps</code>
/// <para>Or install via the NuGet post-build target with the env var <c>PLAYWRIGHT_BROWSERS_PATH</c>.</para>
/// </remarks>
internal sealed class PlaywrightPdfRenderer(IOptions<PlaywrightRendererOptions> options) : IPdfRenderer, IAsyncDisposable
{
    private readonly PlaywrightRendererOptions _options = options.Value;
    private IPlaywright? _playwright;
    private IBrowser?    _browser;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <inheritdoc />
    public async Task<byte[]> RenderAsync(
        string html,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotWhiteSpace(html);

        var browser = await EnsureBrowserAsync(cancellationToken);
        var page    = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            JavaScriptEnabled = _options.JavaScriptEnabled
        }).WaitAsync(cancellationToken);
        try
        {
            // The HTML is set, not navigated to: every request the page makes is a resource it
            // references, and none is allowed. data: URLs are not requests and still load.
            if (!_options.NetworkEnabled)
                await page.RouteAsync("**/*", route => route.AbortAsync()).WaitAsync(cancellationToken);

            await page.SetContentAsync(html, new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            }).WaitAsync(cancellationToken);
            return await page.PdfAsync(MapOptions(options ?? new PdfOptions()))
                .WaitAsync(cancellationToken);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task<IBrowser> EnsureBrowserAsync(CancellationToken ct)
    {
        if (_browser is not null) return _browser;

        await _lock.WaitAsync(ct);
        try
        {
            if (_browser is not null) return _browser;
            _playwright = await global::Microsoft.Playwright.Playwright.CreateAsync().WaitAsync(ct);
            _browser    = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless        = true,
                ChromiumSandbox = _options.Sandbox
            }).WaitAsync(ct);
            return _browser;
        }
        finally { _lock.Release(); }
    }

    private static PagePdfOptions MapOptions(PdfOptions o) => new()
    {
        Format          = o.Format,
        PrintBackground = o.PrintBackground,
        Margin          = o.Margin is null ? null : new Margin
        {
            Top    = o.Margin.Top,
            Bottom = o.Margin.Bottom,
            Left   = o.Margin.Left,
            Right  = o.Margin.Right
        }
    };

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.CloseAsync();
        _playwright?.Dispose();
        _lock.Dispose();
    }
}
