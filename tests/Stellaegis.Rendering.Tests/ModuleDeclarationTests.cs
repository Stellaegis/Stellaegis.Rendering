using Microsoft.Extensions.DependencyInjection;
using Stellaegis.Primitives;
using Stellaegis.Primitives.Hosting;
using Stellaegis.Rendering.Scriban;

namespace Stellaegis.Rendering.Tests;

/// <summary>What the module declares to the application, read against its frame.</summary>
public sealed class ModuleDeclarationTests
{
    [Fact]
    public void The_module_is_declared_once_without_compartments_and_serves_every_frame()
    {
        ApplicationFrame[] frames =
        [
            ApplicationFrame.WithoutCompartments,
            ApplicationFrame.SingleCompartment(Compartment.Parse("acme")),
            ApplicationFrame.ManyCompartments().WithSharedSpace(),
        ];

        foreach (var frame in frames)
        {
            using var provider = new ServiceCollection()
                .AddStellaegisFrame(frame)
                .AddStellaegisRendering()
                .BuildServiceProvider();

            var module = Assert.Single(provider.GetStellaegisModules());
            Assert.Equal(RenderingModule.Name, module.Name);
            Assert.Equal([RenderingOperations.RenderHtml, RenderingOperations.RenderPdf], module.Operations);
            provider.ValidateStellaegisModules();
        }
    }

    [Fact]
    public void A_PDF_engine_declares_the_PDF_capability()
    {
        using var withEngine = new ServiceCollection().AddEngines().BuildServiceProvider();
        using var htmlOnly = new ServiceCollection().AddStellaegisRendering().AddStellaegisScribanRenderer().BuildServiceProvider();

        Assert.Contains(RenderingCapabilities.Pdf, Assert.Single(withEngine.GetStellaegisModules()).Capabilities);
        Assert.Empty(Assert.Single(htmlOnly.GetStellaegisModules()).Capabilities);
    }

    [Fact]
    public void An_application_that_needs_PDF_fails_without_a_PDF_engine()
    {
        using var withEngine = new ServiceCollection().AddEngines()
            .AddStellaegisModuleParameter<RenderingModule>(RenderingCapabilities.Pdf)
            .BuildServiceProvider();
        using var htmlOnly = new ServiceCollection().AddStellaegisRendering().AddStellaegisScribanRenderer()
            .AddStellaegisModuleParameter<RenderingModule>(RenderingCapabilities.Pdf)
            .BuildServiceProvider();

        withEngine.ValidateStellaegisModules();
        var error = Assert.Throws<InvalidOperationException>(htmlOnly.ValidateStellaegisModules);
        Assert.Contains("pdf", error.Message);
    }
}
