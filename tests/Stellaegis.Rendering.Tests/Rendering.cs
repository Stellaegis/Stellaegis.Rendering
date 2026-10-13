using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Stellaegis.Rendering.AspNetCore;
using Stellaegis.Rendering.Client;
using Stellaegis.Rendering.Playwright;
using Stellaegis.Rendering.Scriban;

namespace Stellaegis.Rendering.Tests;

/// <summary>
/// What the tests share: JSON literals, the engines wired as a host wires them, and a renderer in
/// either mode.
/// </summary>
internal static class Rendering
{
    public const string InProcess = "in-process";
    public const string Remote    = "remote";

    public static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement;

    public static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => KeyValuePair.Create<string, string?>(v.Key, v.Value)))
            .Build();

    /// <summary>The in-process renderer over Scriban and Playwright, as a monolith registers it.</summary>
    public static IServiceCollection AddEngines(this IServiceCollection services, IConfiguration? configuration = null) =>
        services.AddStellaegisRendering()
            .AddStellaegisScribanRenderer()
            .AddStellaegisPlaywrightRenderer(configuration ?? Configuration());

    /// <summary>A renderer in the given mode; disposing it stops whatever it started.</summary>
    public static async Task<Session> StartAsync(string mode) => mode switch
    {
        InProcess => Session.Of(new ServiceCollection().AddEngines().BuildServiceProvider()),
        Remote    => await StartRemoteAsync(app => app.MapGroup("/api").MapRenderingEndpointsAnonymous()),
        _         => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };

    /// <summary>
    /// An in-memory host mapped by <paramref name="map"/>, and the HTTP client pointed at it — the two
    /// ends of the wire.
    /// </summary>
    public static async Task<Session> StartRemoteAsync(Action<WebApplication> map, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddEngines();
        configure?.Invoke(builder);

        var app = builder.Build();
        map(app);
        await app.StartAsync(TestContext.Current.CancellationToken);

        var client = new ServiceCollection();
        client.AddStellaegisRenderingClient(Configuration(("Stellaegis:Rendering:Client:BaseUrl", "http://localhost/api/rendering/")))
            .ConfigurePrimaryHttpMessageHandler(() => app.GetTestServer().CreateHandler());

        return Session.Of(client.BuildServiceProvider(), app);
    }

    /// <summary>A renderer and what has to be stopped with it.</summary>
    public sealed class Session(ITemplateRenderer renderer, params IAsyncDisposable[] owned) : IAsyncDisposable
    {
        public ITemplateRenderer Renderer { get; } = renderer;

        public HttpClient? HttpClient { get; init; }

        public static Session Of(ServiceProvider services, WebApplication? app = null) =>
            app is null
                ? new Session(services.GetRequiredService<ITemplateRenderer>(), services)
                : new Session(services.GetRequiredService<ITemplateRenderer>(), services, app)
                {
                    HttpClient = app.GetTestServer().CreateClient()
                };

        public async ValueTask DisposeAsync()
        {
            HttpClient?.Dispose();
            foreach (var resource in owned)
                await resource.DisposeAsync();
        }
    }
}

/// <summary>
/// Whether Playwright's Chromium can be launched here. When it cannot, <see cref="Unavailable"/> says
/// why, and the tests that need it are skipped rather than failed.
/// </summary>
public sealed class ChromiumFixture : IAsyncLifetime
{
    /// <summary>Why Chromium cannot be used, or <c>null</c> when it can.</summary>
    public string? Unavailable { get; private set; }

    public async ValueTask InitializeAsync()
    {
        try
        {
            using var playwright = await global::Microsoft.Playwright.Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync();
        }
        catch (PlaywrightException e)
        {
            Unavailable = $"No Chromium for the PDF tests — install it with playwright.ps1 install chromium: {e.Message}";
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
