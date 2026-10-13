using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using static Stellaegis.Rendering.Tests.Rendering;

namespace Stellaegis.Rendering.Tests;

/// <summary>
/// Chromium prints the HTML it is given on the host: by default it runs no JavaScript and makes no
/// request. Each refusal is checked against its control — the same page, the switch open — so that
/// a probe that sees nothing proves the confinement, not a broken probe.
/// </summary>
public sealed class ChromiumConfinementTests(ChromiumFixture chromium) : IClassFixture<ChromiumFixture>
{
    [Fact]
    public async Task By_default_a_referenced_resource_is_not_requested()
    {
        Assert.SkipWhen(chromium.Unavailable is not null, chromium.Unavailable ?? "");
        await using var probe = Probe.Start();

        await PrintAsync($"""<img src="{probe.Url}/image">""");

        Assert.Equal(0, probe.Requests);
    }

    [Fact]
    public async Task With_the_network_enabled_a_referenced_resource_is_requested()
    {
        Assert.SkipWhen(chromium.Unavailable is not null, chromium.Unavailable ?? "");
        await using var probe = Probe.Start();

        await PrintAsync($"""<img src="{probe.Url}/image">""", ("NetworkEnabled", "true"));

        Assert.True(probe.Requests > 0);
    }

    [Fact]
    public async Task By_default_a_script_does_not_run()
    {
        Assert.SkipWhen(chromium.Unavailable is not null, chromium.Unavailable ?? "");
        await using var probe = Probe.Start();

        await PrintAsync(ScriptCalling(probe), ("NetworkEnabled", "true"));

        Assert.Equal(0, probe.Requests);
    }

    [Fact]
    public async Task With_JavaScript_enabled_a_script_runs()
    {
        Assert.SkipWhen(chromium.Unavailable is not null, chromium.Unavailable ?? "");
        await using var probe = Probe.Start();

        await PrintAsync(ScriptCalling(probe), ("NetworkEnabled", "true"), ("JavaScriptEnabled", "true"));

        Assert.True(probe.Requests > 0);
    }

    [Fact]
    public async Task An_embedded_data_URL_image_still_prints()
    {
        Assert.SkipWhen(chromium.Unavailable is not null, chromium.Unavailable ?? "");
        const string pixel = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

        var pdf = await PrintAsync($"""<img src="data:image/png;base64,{pixel}">""");

        Assert.Contains("/Image", Encoding.Latin1.GetString(pdf));
    }

    private static string ScriptCalling(Probe probe) =>
        $"""<script>new Image().src = "{probe.Url}/script";</script>""";

    private static async Task<byte[]> PrintAsync(string body, params (string Key, string Value)[] settings)
    {
        var configuration = Configuration(settings.Select(s => ($"Stellaegis:Rendering:Playwright:{s.Key}", s.Value)).ToArray());
        await using var services = new ServiceCollection().AddEngines(configuration).BuildServiceProvider();

        return await services.GetRequiredService<IPdfRenderer>().RenderAsync(
            $"<html><body><p>confinement</p>{body}</body></html>",
            cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// A loopback TCP listener counting the connections made to it, each answered <c>204</c>: a
    /// request Chromium makes is a connection here.
    /// </summary>
    private sealed class Probe : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _stop = new();
        private readonly Task _loop;
        private int _requests;

        private Probe()
        {
            _listener.Start();
            Url   = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}";
            _loop = AcceptAsync();
        }

        public string Url { get; }

        public int Requests => Volatile.Read(ref _requests);

        public static Probe Start() => new();

        private async Task AcceptAsync()
        {
            var answer = Encoding.ASCII.GetBytes("HTTP/1.1 204 No Content\r\nConnection: close\r\n\r\n");
            try
            {
                while (true)
                {
                    using var client = await _listener.AcceptTcpClientAsync(_stop.Token);
                    Interlocked.Increment(ref _requests);
                    var stream = client.GetStream();
                    await ReadHeadersAsync(stream, _stop.Token);
                    await stream.WriteAsync(answer, _stop.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static async Task ReadHeadersAsync(NetworkStream stream, CancellationToken ct)
        {
            var buffer = new byte[4096];
            var total  = 0;
            while (total < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(total), ct);
                if (read == 0) return;
                total += read;
                if (Encoding.ASCII.GetString(buffer, 0, total).Contains("\r\n\r\n")) return;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _stop.CancelAsync();
            _listener.Stop();
            await _loop;
            _stop.Dispose();
        }
    }
}
