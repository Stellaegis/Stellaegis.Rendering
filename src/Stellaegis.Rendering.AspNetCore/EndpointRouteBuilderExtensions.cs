using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Stellaegis.Primitives;
using Stellaegis.Primitives.AspNetCore;
using Stellaegis.Primitives.Hosting;

namespace Stellaegis.Rendering.AspNetCore;

/// <summary>
/// Maps the rendering API: <see cref="ITemplateRenderer"/> exposed over HTTP, for the client in
/// <c>Stellaegis.Rendering.Client</c>.
/// </summary>
/// <remarks>
/// <para>
/// Two endpoints, relative to the builder they are mapped on, each labelled with its operation from
/// <see cref="RenderingOperations"/>:
/// </para>
/// <list type="bullet">
///   <item><c>POST rendering/html</c> — render a template to HTML</item>
///   <item><c>POST rendering/pdf</c>  — render a template to PDF, in a single call; mapped only when a PDF
///   engine declares <see cref="RenderingCapabilities.Pdf"/></item>
/// </list>
/// <para>
/// They render with whatever <see cref="ITemplateRenderer"/> the host registers — in practice the
/// in-process one, over the engines registered beside it. They load no resource: the request carries
/// its template and its data. What the caller gets wrong — a blank template, a template at fault, PDF options no
/// engine prints — is answered as a <c>400</c> problem by the error translation of Stellaegis.Primitives, with the
/// code the client recognises.
/// </para>
/// <para>
/// Rendering a PDF runs a browser on the host, so access is never left implicit: the endpoints are
/// closed until the host declares their authorization on the returned group, and the startup check
/// (<see cref="ServiceCollectionExtensions.AddStellaegisRenderingEndpoints"/>) fails a host that
/// declares none. Everything else about the host — authentication schemes, CORS, rate limiting,
/// forwarded headers, health checks — is the caller's to configure.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// builder.Services.AddStellaegisRendering()
///     .AddStellaegisScribanRenderer()
///     .AddStellaegisPlaywrightRenderer(builder.Configuration)
///     .AddStellaegisRenderingEndpoints();
/// builder.Services.AddAuthentication(...);
/// builder.Services.AddAuthorization(...);
///
/// var app = builder.Build();
/// app.UseAuthentication();
/// app.UseAuthorization();
/// app.MapGroup("/api").MapRenderingEndpoints()
///    .RequireAuthorizationPerOperation(op => op.ToString());   // "templates.render_html", "templates.render_pdf"
/// </code>
/// </example>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the rendering endpoints, each labelled with its operation from <see cref="RenderingOperations"/>,
    /// and none secured: the host attaches its security to the returned group. The PDF route is mapped only when a
    /// registered PDF engine declares <see cref="RenderingCapabilities.Pdf"/>: a host without one has no such route.
    /// </summary>
    /// <param name="endpoints">The builder to map the endpoints on — the application, or a route group.</param>
    /// <returns>The group holding the endpoints, for the host's security and further conventions.</returns>
    public static RouteGroupBuilder MapRenderingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Guard.NotNull(endpoints);

        var group = endpoints.MapGroup("rendering")
            .BelongsTo<RenderingModule>();
        group.AddErrorTranslation();

        group.MapPost("/html", async (
                RenderHtmlRequest request,
                ITemplateRenderer renderer,
                CancellationToken ct) =>
            {
                var rendered = await renderer.RenderHtmlAsync(request.Template, request.Data, ct);
                return Results.Text(rendered, "text/html");
            })
            .WithMetadata(RenderingOperations.RenderHtml)
            .LoadsNoResource()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        if (!HoldsPdf(endpoints.ServiceProvider))
            return group;

        group.MapPost("/pdf", async (
                RenderPdfRequest request,
                ITemplateRenderer renderer,
                CancellationToken ct) =>
            {
                var bytes = await renderer.RenderPdfAsync(request.Template, request.Data, request.Options, ct);
                return Results.Bytes(bytes, "application/pdf");
            })
            .WithMetadata(RenderingOperations.RenderPdf)
            .LoadsNoResource()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return group;
    }

    private static bool HoldsPdf(IServiceProvider services) =>
        services.GetStellaegisModule<RenderingModule>()?.Capabilities.Contains(RenderingCapabilities.Pdf) == true;
}
