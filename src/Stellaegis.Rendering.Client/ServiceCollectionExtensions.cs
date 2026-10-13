using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Stellaegis.Rendering.Client;

/// <summary>
/// Dependency injection helpers for Stellaegis.Rendering.Client.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ITemplateRenderer"/> backed by an HTTP client targeting the rendering API.
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
        string sectionName = RenderingClientOptions.SectionName)
    {
        services.Configure<RenderingClientOptions>(configuration.GetSection(sectionName));

        return services.AddHttpClient<ITemplateRenderer, HttpTemplateRenderer>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<RenderingClientOptions>>().Value;
            http.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
            http.Timeout     = opts.Timeout;
        });
    }
}
