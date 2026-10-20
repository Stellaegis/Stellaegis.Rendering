using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stellaegis.Primitives.Hosting;

namespace Stellaegis.Rendering.Client;

/// <summary>
/// Dependency injection helpers for Stellaegis.Rendering.Client.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ITemplateRenderer"/> backed by an HTTP client targeting the rendering API, its options
    /// bound from <paramref name="sectionName"/> and validated when the host starts.
    /// </summary>
    /// <remarks>
    /// The client sends requests as they are: authenticating them is the caller's concern. Add a
    /// <see cref="System.Net.Http.DelegatingHandler"/> to the returned builder to attach a token, an
    /// API key, or anything else the rendering host requires.
    /// </remarks>
    /// <example>
    /// appsettings.json:
    /// <code>
    /// "Stellaegis": {
    ///   "Rendering": {
    ///     "Client": { "BaseUrl": "https://rendering.example.com/api/rendering/" }
    ///   }
    /// }
    /// </code>
    /// Program.cs:
    /// <code>
    /// builder.Services.AddStellaegisRenderingClient(builder.Configuration)
    ///     .AddHttpMessageHandler&lt;MyTokenHandler&gt;();
    /// </code>
    /// </example>
    /// <returns>The builder of the underlying HTTP client, to add handlers to.</returns>
    public static IHttpClientBuilder AddStellaegisRenderingClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = RenderingClientOptions.SectionName) =>
        services.AddStellaegisHttpClient<ITemplateRenderer, HttpTemplateRenderer, RenderingClientOptions>(configuration, sectionName);

    /// <summary>
    /// Registers <see cref="ITemplateRenderer"/> backed by an HTTP client targeting the rendering API, its options
    /// configured in code and validated when the host starts.
    /// </summary>
    /// <returns>The builder of the underlying HTTP client, to add handlers to.</returns>
    public static IHttpClientBuilder AddStellaegisRenderingClient(
        this IServiceCollection services,
        Action<RenderingClientOptions> configure) =>
        services.AddStellaegisHttpClient<ITemplateRenderer, HttpTemplateRenderer, RenderingClientOptions>(configure);
}
