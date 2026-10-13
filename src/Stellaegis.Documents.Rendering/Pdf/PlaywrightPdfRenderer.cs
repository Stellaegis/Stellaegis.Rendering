using Microsoft.Playwright;

namespace Stellaegis.Documents.Rendering.Pdf;

/// <summary>
/// Renders HTML to PDF using a shared Playwright Chromium browser instance.
/// Registered as a singleton; the browser is lazily initialised on first use.
/// </summary>
/// <remarks>
/// <para><b>Docker setup:</b> the Playwright Chromium driver must be installed in the image:</para>
/// <code>RUN dotnet tool install --global Microsoft.Playwright.CLI &amp;&amp; playwright install chromium --with-deps</code>
/// <para>Or install via the NuGet post-build target with the env var <c>PLAYWRIGHT_BROWSERS_PATH</c>.</para>
/// </remarks>
public sealed class PlaywrightPdfRenderer : IPdfRenderer, IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser?    _browser;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <inheritdoc />
    public async Task<byte[]> RenderAsync(
        string html,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var browser = await EnsureBrowserAsync(cancellationToken);
        var page    = await browser.NewPageAsync().WaitAsync(cancellationToken);
        try
        {
            await page.SetContentAsync(html, new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            }).WaitAsync(cancellationToken);
            return await page.PdfAsync(
                options is null ? new PagePdfOptions() : MapOptions(options))
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
            _playwright = await Playwright.CreateAsync().WaitAsync(ct);
            _browser    = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args     = ["--no-sandbox", "--disable-setuid-sandbox"]
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
