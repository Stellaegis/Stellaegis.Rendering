using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Stellaegis.Http;

namespace Stellaegis.Documents.Client;

/// <summary>
/// Dependency injection helpers for Stellaegis.Documents.Client.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IDocumentsClient"/> with M2M authentication via <see cref="M2MTokenProvider"/>.
    /// Requires <see cref="M2MTokenProvider"/> to be registered (call <c>AddStellaegisM2M()</c> first).
    /// </summary>
    /// <example>
    /// appsettings.json:
    /// <code>
    /// "Stellaegis": {
    ///   "M2M": { "TokenEndpoint": "...", "ClientId": "...", "ClientSecret": "..." },
    ///   "Documents": { "BaseUrl": "https://rendering.api.stellaegis.local/api/rendering/", "Scope": "rendering" }
    /// }
    /// </code>
    /// Program.cs:
    /// <code>
    /// builder.Services.AddStellaegisM2M(builder.Configuration);
    /// builder.Services.AddStellaegisDocumentsClient(builder.Configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddStellaegisDocumentsClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = DocumentsClientOptions.SectionName)
    {
        if (!services.Any(d => d.ServiceType == typeof(M2MTokenProvider)))
            throw new InvalidOperationException(
                "M2MTokenProvider is not registered. Call AddStellaegisM2M() before AddStellaegisDocumentsClient().");

        services.Configure<DocumentsClientOptions>(configuration.GetSection(sectionName));

        services.AddHttpClient<IDocumentsClient, DocumentsClient>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<DocumentsClientOptions>>().Value;
            http.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
            http.Timeout     = opts.Timeout;
        })
        .AddHttpMessageHandler(sp =>
        {
            var opts     = sp.GetRequiredService<IOptions<DocumentsClientOptions>>().Value;
            var provider = sp.GetRequiredService<M2MTokenProvider>();
            return new M2MTokenHandler(provider, opts.Scope);
        });

        return services;
    }
}
