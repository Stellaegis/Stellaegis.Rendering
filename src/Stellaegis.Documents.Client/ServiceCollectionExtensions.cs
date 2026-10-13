using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Stellaegis.Documents.Client;

/// <summary>
/// Dependency injection helpers for Stellaegis.Documents.Client.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IDocumentsClient"/> backed by an HTTP client targeting the rendering API.
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
    /// builder.Services.AddStellaegisDocumentsClient(builder.Configuration)
    ///     .AddHttpMessageHandler&lt;MyTokenHandler&gt;();
    /// </code>
    /// </example>
    /// <returns>The builder of the underlying HTTP client, to add handlers to.</returns>
    public static IHttpClientBuilder AddStellaegisDocumentsClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = DocumentsClientOptions.SectionName)
    {
        services.Configure<DocumentsClientOptions>(configuration.GetSection(sectionName));

        return services.AddHttpClient<IDocumentsClient, DocumentsClient>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<DocumentsClientOptions>>().Value;
            http.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
            http.Timeout     = opts.Timeout;
        });
    }
}
