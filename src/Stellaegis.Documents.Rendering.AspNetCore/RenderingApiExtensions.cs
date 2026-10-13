using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Stellaegis.AspNetCore;
using Stellaegis.Documents.Rendering.Pdf;

namespace Stellaegis.Documents.Rendering.AspNetCore;

/// <summary>
/// Extension methods that configure the stateless Stellaegis document-rendering API host:
/// M2M authentication (scope <c>"rendering"</c>), CORS, JSON serialization, rate limiting,
/// the HTML/PDF render endpoints, and a health check.
/// </summary>
/// <remarks>
/// This is the M2M, server-to-server half of the documents domain — the counterpart of the
/// tenant-scoped content host (<c>Stellaegis.Documents.AspNetCore</c>). It owns no data: each
/// request carries its own Scriban template and JSON data, so the host touches neither
/// <c>doc.documents</c> nor the GED blob store. Callers reach it through the
/// <c>IDocumentsClient</c> M2M client (package <c>Stellaegis.Documents.Client</c>).
/// </remarks>
/// <example>
/// <code>
/// var builder = WebApplication.CreateBuilder(args);
/// builder.Services.AddStellaegisDocumentRendering();   // Scriban + Playwright renderers
/// builder.AddStellaegisRenderingApi();
///
/// var app = builder.Build();
/// app.UseStellaegisRenderingApi();
/// app.Run();
/// </code>
/// </example>
public static class RenderingApiExtensions
{
    private const string HealthPath = "/api/rendering/health";

    /// <summary>
    /// Registers all services required by the rendering API host: M2M authentication
    /// (scope <c>"rendering"</c>), CORS, JSON serialization, and rate limiting.
    /// </summary>
    /// <remarks>
    /// Call <c>AddStellaegisDocumentRendering()</c> (which registers <see cref="IHtmlRenderer"/>
    /// and <see cref="IPdfRenderer"/>) <b>before</b> this method.
    /// </remarks>
    public static WebApplicationBuilder AddStellaegisRenderingApi(this WebApplicationBuilder builder)
    {
        builder.AddStellaegisM2MAuthentication("rendering");
        builder.Services.AddStellaegisCors(builder.Configuration, builder.Environment);
        builder.Services.AddStellaegisJsonOptions();
        builder.Services.AddStellaegisRateLimiter(builder.Configuration);

        // No seeders run here (stateless host), but /health/ready needs SeedingState.
        builder.Services.AddStellaegisSeedingState();

        return builder;
    }

    /// <summary>
    /// Configures the middleware pipeline and maps the rendering endpoints.
    /// </summary>
    public static WebApplication UseStellaegisRenderingApi(this WebApplication app)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseStellaegisExceptionHandler();
        app.UseStellaegisSecurityHeaders();
        app.UseRateLimiter();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStellaegisHealth();
        app.MapStellaegisRenderingEndpoints();

        return app;
    }

    /// <summary>
    /// Maps the rendering endpoints without configuring the middleware pipeline.
    /// Use this when composing several APIs in a single host:
    /// <list type="bullet">
    ///   <item><c>POST /api/rendering/html</c> — render a Scriban template to HTML</item>
    ///   <item><c>POST /api/rendering/pdf</c>  — render a Scriban template to PDF</item>
    ///   <item><c>GET  /api/rendering/health</c> — health check (anonymous)</item>
    /// </list>
    /// </summary>
    public static WebApplication MapStellaegisRenderingEndpoints(this WebApplication app)
    {
        app.MapGet(HealthPath, () => Results.Ok(new
            {
                api = "rendering",
                status = "healthy",
                timestamp = DateTimeOffset.UtcNow
            }))
            .WithName("HealthCheck_Rendering")
            .AllowAnonymous();

        var group = app.MapGroup("/api/rendering")
            .RequireAuthorization(StellaegisM2MDefaults.PolicyName);

        group.MapPost("/html", async (
                RenderRequest request,
                IHtmlRenderer html,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(request.Template))
                    return Results.BadRequest("A non-empty 'template' is required.");

                var rendered = await html.RenderAsync(request.Template, request.Data, ct);
                return Results.Text(rendered, "text/html");
            })
            .WithName("RenderHtml");

        group.MapPost("/pdf", async (
                RenderRequest request,
                IHtmlRenderer html,
                IPdfRenderer pdf,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(request.Template))
                    return Results.BadRequest("A non-empty 'template' is required.");

                var rendered = await html.RenderAsync(request.Template, request.Data, ct);
                var bytes = await pdf.RenderAsync(rendered, cancellationToken: ct);
                return Results.Bytes(bytes, "application/pdf");
            })
            .WithName("RenderPdf");

        return app;
    }
}

/// <summary>
/// Render request payload — a Scriban template and the JSON data model bound to it.
/// Mirrors the contract emitted by the <c>IDocumentsClient</c> M2M client.
/// </summary>
public sealed record RenderRequest(string Template, JsonElement Data);
