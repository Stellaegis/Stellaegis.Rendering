using System.Net;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stellaegis.Primitives;
using Stellaegis.Primitives.AspNetCore;
using Stellaegis.Rendering.AspNetCore;
using static Stellaegis.Rendering.Tests.Rendering;

namespace Stellaegis.Rendering.Tests;

/// <summary>
/// The endpoints and what the client makes of their answers, in one in-memory host.
/// </summary>
public sealed class RenderingApiTests
{
    [Fact]
    public async Task Endpoints_behind_a_policy_refuse_an_unauthenticated_caller()
    {
        await using var session = await StartRemoteAsync(
            app =>
            {
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapGroup("/api").MapRenderingEndpoints().RequireAuthorization("rendering");
            },
            builder =>
            {
                builder.Services.AddAuthentication(NeverAuthenticated.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, NeverAuthenticated>(NeverAuthenticated.SchemeName, null);
                builder.Services.AddAuthorization(o => o.AddPolicy("rendering", p => p.RequireAuthenticatedUser()));
            });

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => session.Renderer.RenderHtmlAsync(
            "{{ v }}", Json("{}"), TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
    }

    [Theory]
    [InlineData("""{"template":" ","data":{}}""", "'template'")]
    [InlineData("""{"template":"{{ v }}","data":[1]}""", "'data'")]
    public async Task A_request_the_engine_would_refuse_is_answered_400_naming_the_argument_in_development_only(
        string body, string named)
    {
        var ct = TestContext.Current.CancellationToken;

        foreach (var environment in new[] { Environments.Production, Environments.Development })
        {
            await using var session = await StartRemoteAsync(
                app => app.MapGroup("/api").MapRenderingEndpoints().AllowAnonymous(),
                environment: environment);

            foreach (var route in new[] { "/api/rendering/html", "/api/rendering/pdf" })
            {
                using var content  = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                using var response = await session.HttpClient!.PostAsync(route, content, ct);
                var answer = await response.Content.ReadAsStringAsync(ct);

                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
                Assert.Equal(environment == Environments.Development, answer.Contains(named));
            }
        }
    }

    /// <remarks>
    /// Checked on the raw answer: the in-memory host throws an exception the endpoint lets escape
    /// straight into the client, so a client-side test alone would pass without the 400.
    /// </remarks>
    [Fact]
    public async Task A_template_at_fault_is_answered_400_with_its_code_and_its_problem_type()
    {
        await using var session = await StartRemoteAsync(app => app.MapGroup("/api").MapRenderingEndpoints().AllowAnonymous());
        var ct = TestContext.Current.CancellationToken;

        using var response = await session.HttpClient!.PostAsJsonAsync(
            "/api/rendering/html", new { template = "{{ if }}", data = new { } }, ct);
        using var answer = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(TemplateException.Code, answer.RootElement.GetProperty("code").GetString());
        Assert.Equal(TemplateException.ProblemType, answer.RootElement.GetProperty("type").GetString());
        Assert.StartsWith("Template parse error(s):", answer.RootElement.GetProperty("detail").GetString());
    }

    /// <remarks>
    /// The rendering API answers the problem type beside the code, which would hide a client that no longer reads
    /// the code: this host answers the code alone, as the error translation does.
    /// </remarks>
    [Fact]
    public async Task A_template_at_fault_is_recognised_by_its_code()
    {
        await using var session = await StartRemoteAsync(app =>
            app.MapPost("/api/rendering/html", () => Results.Problem(
                Error.Validation(TemplateException.Code, "Template parse error(s): answered with its code alone.")
                    .ToProblemDetails())));

        var error = await Assert.ThrowsAsync<TemplateException>(() => session.Renderer.RenderHtmlAsync(
            "{{ if }}", Json("{}"), TestContext.Current.CancellationToken));
        Assert.Equal("Template parse error(s): answered with its code alone.", error.Message);
    }

    [Fact]
    public async Task A_template_at_fault_from_a_4_0_host_is_still_recognised()
    {
        await using var session = await StartRemoteAsync(app =>
            app.MapPost("/api/rendering/html", () => Results.Problem(
                detail: "Template parse error(s): as a 4.0 host answers it.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "The template is at fault.",
                type: TemplateException.ProblemType)));

        var error = await Assert.ThrowsAsync<TemplateException>(() => session.Renderer.RenderHtmlAsync(
            "{{ if }}", Json("{}"), TestContext.Current.CancellationToken));
        Assert.Equal("Template parse error(s): as a 4.0 host answers it.", error.Message);
    }

    [Fact]
    public async Task A_failure_keeps_what_the_host_answered()
    {
        await using var session = await StartRemoteAsync(app =>
            app.MapPost("/api/rendering/html", () => Results.BadRequest("The host says why.")));

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => session.Renderer.RenderHtmlAsync(
            "{{ v }}", Json("{}"), TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        Assert.Contains("The host says why.", error.Message);
    }

    /// <summary>An authentication scheme that never authenticates anyone.</summary>
    private sealed class NeverAuthenticated(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "never";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());
    }
}
