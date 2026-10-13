using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Stellaegis.Rendering.AspNetCore;

/// <summary>
/// Maps the rendering API: <see cref="ITemplateRenderer"/> exposed over HTTP, for the client in
/// <c>Stellaegis.Rendering.Client</c>.
/// </summary>
/// <remarks>
/// <para>
/// Two endpoints, relative to the builder they are mapped on:
/// </para>
/// <list type="bullet">
///   <item><c>POST rendering/html</c> — render a template to HTML</item>
///   <item><c>POST rendering/pdf</c>  — render a template to PDF, in a single call</item>
/// </list>
/// <para>
/// They render with whatever <see cref="ITemplateRenderer"/> the host registers — in practice the
/// in-process one, over the engines registered beside it.
/// </para>
/// <para>
/// Each request carries its own template, and rendering a PDF runs a browser on the host, so access
/// is never left implicit: either name the authorization policy that protects the endpoints, or open
/// them deliberately with <see cref="MapRenderingEndpointsAnonymous"/>. Everything else about the
/// host — authentication schemes, CORS, rate limiting, forwarded headers, health checks — is the
/// caller's to configure.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// builder.Services.AddStellaegisRendering()
///     .AddStellaegisScribanRenderer()
///     .AddStellaegisPlaywrightRenderer(builder.Configuration);
/// builder.Services.AddAuthentication(...);
/// builder.Services.AddAuthorization(o => o.AddPolicy("rendering", p => ...));
///
/// var app = builder.Build();
/// app.UseAuthentication();
/// app.UseAuthorization();
/// app.MapGroup("/api").MapRenderingEndpoints(authorizationPolicy: "rendering");
/// </code>
/// </example>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the rendering endpoints, each call required to satisfy <paramref name="authorizationPolicy"/>.
    /// </summary>
    /// <param name="endpoints">The builder to map the endpoints on — the application, or a route group.</param>
    /// <param name="authorizationPolicy">Name of the authorization policy every call must satisfy.</param>
    /// <returns>The group holding the endpoints, for further conventions.</returns>
    public static RouteGroupBuilder MapRenderingEndpoints(this IEndpointRouteBuilder endpoints, string authorizationPolicy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationPolicy);
        return MapEndpoints(endpoints).RequireAuthorization(authorizationPolicy);
    }

    /// <summary>
    /// Maps the rendering endpoints with no authorization at all: anyone who can reach the host can
    /// have it render any template. Meant for hosts reachable only from a trusted network.
    /// </summary>
    /// <param name="endpoints">The builder to map the endpoints on — the application, or a route group.</param>
    /// <returns>The group holding the endpoints, for further conventions.</returns>
    public static RouteGroupBuilder MapRenderingEndpointsAnonymous(this IEndpointRouteBuilder endpoints) =>
        MapEndpoints(endpoints).AllowAnonymous();

    private static RouteGroupBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("rendering");

        group.MapPost("/html", async (
                RenderHtmlRequest request,
                ITemplateRenderer renderer,
                CancellationToken ct) =>
            {
                if (Validate(request.Template, request.Data) is { } error)
                    return Results.BadRequest(error);

                var rendered = await renderer.RenderHtmlAsync(request.Template, request.Data, ct);
                return Results.Text(rendered, "text/html");
            })
            .WithName("RenderHtml");

        group.MapPost("/pdf", async (
                RenderPdfRequest request,
                ITemplateRenderer renderer,
                CancellationToken ct) =>
            {
                if (Validate(request.Template, request.Data) is { } error)
                    return Results.BadRequest(error);

                var bytes = await renderer.RenderPdfAsync(request.Template, request.Data, request.Options, ct);
                return Results.Bytes(bytes, "application/pdf");
            })
            .WithName("RenderPdf");

        return group;
    }

    private static string? Validate(string? template, JsonElement data)
    {
        if (string.IsNullOrWhiteSpace(template))
            return "A non-empty 'template' is required.";
        if (data.ValueKind != JsonValueKind.Object)
            return "'data' must be a JSON object.";
        return null;
    }
}
