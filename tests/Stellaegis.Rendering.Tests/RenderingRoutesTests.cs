using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stellaegis.Primitives;
using Stellaegis.Primitives.AspNetCore;
using Stellaegis.Rendering.AspNetCore;
using Stellaegis.Rendering.Client;
using static Stellaegis.Rendering.Tests.Rendering;

namespace Stellaegis.Rendering.Tests;

/// <summary>
/// What the routes declare — their module, their operation, that they load no resource — and what the startup
/// check makes of a host that maps them.
/// </summary>
public sealed class RenderingRoutesTests
{
    private static WebApplication Host(Action<IServiceCollection> register)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        register(builder.Services);
        return builder.Build();
    }

    private static void Engines(IServiceCollection services) => services.AddEngines().AddStellaegisRenderingEndpoints();

    [Fact]
    public async Task Each_route_carries_its_operation_and_belongs_to_the_module()
    {
        await using var app = Host(Engines);
        app.MapGroup("/api").MapRenderingEndpoints().AllowAnonymous();
        await app.StartAsync(TestContext.Current.CancellationToken);

        var operations = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints).OfType<RouteEndpoint>()
            .ToDictionary(endpoint => endpoint.RoutePattern.RawText!, endpoint => endpoint.GetOperation());

        Assert.Equal(RenderingOperations.RenderHtml, operations["/api/rendering/html"]);
        Assert.Equal(RenderingOperations.RenderPdf, operations["/api/rendering/pdf"]);
        Assert.All(operations.Values, operation => Assert.Equal(OperationKind.Read, operation!.Kind));
        Assert.Equal("templates.render_html", RenderingOperations.RenderHtml.ToString());
    }

    [Fact]
    public async Task Routes_without_authorization_fail_the_startup()
    {
        await using var app = Host(Engines);
        app.MapGroup("/api").MapRenderingEndpoints();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync(TestContext.Current.CancellationToken));
        Assert.Contains("declare no authorization", error.Message);
    }

    [Fact]
    public async Task A_host_that_does_not_declare_the_module_fails_the_startup()
    {
        // A host relaying to a remote renderer: the endpoints are mapped, AddStellaegisRendering is not called.
        await using var app = Host(services =>
        {
            services.AddStellaegisRenderingClient(Configuration(("Stellaegis:Rendering:Client:BaseUrl", "http://localhost/")));
            services.AddStellaegisRenderingEndpoints();
        });
        app.MapGroup("/api").MapRenderingEndpoints().AllowAnonymous();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync(TestContext.Current.CancellationToken));
        Assert.Contains("modules nothing declares: rendering", error.Message);
    }

    [Fact]
    public void Mapping_the_routes_without_their_registration_fails()
    {
        var app = Host(services => services.AddEngines());

        var error = Assert.Throws<InvalidOperationException>(() => app.MapRenderingEndpoints());
        Assert.Contains("the check is not registered", error.Message);
    }

    [Fact]
    public async Task A_policy_per_operation_protects_PDF_apart_from_HTML()
    {
        await using var app = Host(services =>
        {
            Engines(services);
            services.AddAuthentication(EveryoneAuthenticated.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, EveryoneAuthenticated>(EveryoneAuthenticated.SchemeName, null);
            services.AddAuthorization(o =>
            {
                o.AddPolicy(RenderingOperations.RenderHtml.ToString(), p => p.RequireAssertion(_ => true));
                o.AddPolicy(RenderingOperations.RenderPdf.ToString(), p => p.RequireAssertion(_ => false));
            });
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGroup("/api").MapRenderingEndpoints().RequireAuthorizationPerOperation(op => op.ToString());
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var http = app.GetTestServer().CreateClient();

        var request = new { template = "{{ v }}", data = new { v = "x" } };
        using var html = await http.PostAsJsonAsync("/api/rendering/html", request, TestContext.Current.CancellationToken);
        using var pdf = await http.PostAsJsonAsync("/api/rendering/pdf", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, html.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, pdf.StatusCode);
    }

    /// <summary>An authentication scheme that authenticates everyone.</summary>
    private sealed class EveryoneAuthenticated(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "everyone";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "someone")], SchemeName));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
        }
    }
}
