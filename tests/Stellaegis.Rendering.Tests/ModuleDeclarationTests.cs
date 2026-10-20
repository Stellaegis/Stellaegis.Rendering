using Microsoft.Extensions.DependencyInjection;
using Stellaegis.Primitives;
using Stellaegis.Primitives.DependencyInjection;

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
            provider.ValidateStellaegisModules();
        }
    }
}
