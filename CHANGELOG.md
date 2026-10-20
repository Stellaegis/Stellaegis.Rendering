# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [4.0.0] — 2026-10-20

Rendering takes the shape of a Stellaegis 4 module: its routes carry their operations and stay closed
until the host secures them, and PDF becomes a capability that an application can require. The
version moves from 1.0.1 to 4.0.0 to align with the Stellaegis family.

### Added

- `RenderingModule`, named `rendering`, which `AddStellaegisRendering` declares as a module without
  compartments: every application frame accepts it.
- `RenderingOperations`: `templates.render_html` and `templates.render_pdf`, both of kind `Read`. A
  host protects PDF apart from HTML with one policy per operation,
  `.RequireAuthorizationPerOperation(op => op.ToString())`.
- `RenderingCapabilities.Pdf`, declared by `AddStellaegisPlaywrightRenderer`. An application that
  needs PDF turns it on with `AddStellaegisModuleParameter<RenderingModule>(RenderingCapabilities.Pdf)`,
  and fails at startup rather than on its first PDF when no PDF engine is registered.
- `AddStellaegisRenderingEndpoints`, which registers the startup check of the routes.

### Changed

- `MapRenderingEndpoints()` takes no policy. It returns the group, on which the host declares the
  authorization; a host that maps the routes without any fails at startup. The routes load no
  resource, so they ask for no authorization handler, and they carry no endpoint name.
- The PDF route is mapped only when a PDF engine is registered.
- An invalid request — a blank template, data that is not a JSON object — is answered as a `400`
  problem, whose detail is shown in `Development` only. It was a bare string. The client checks the
  same arguments before sending, and still raises `ArgumentException` with its message.
- Stellaegis.Primitives 4.1.0. The core also references Stellaegis.Primitives.DependencyInjection, and
  `.AspNetCore` Stellaegis.Primitives.AspNetCore.
- Scriban 7.5.1.

### Removed

- `MapRenderingEndpointsAnonymous`: map with `MapRenderingEndpoints().AllowAnonymous()`.

### Updating from 1.0.1

A process that renders in process, or through the client, changes nothing, provided it calls
`AddStellaegisRendering` once: it now declares the module, and a module declared twice fails the
startup. A host that serves the endpoints registers them and declares their authorization on the group:

```csharp
builder.Services.AddStellaegisRendering()
    .AddStellaegisScribanRenderer()
    .AddStellaegisPlaywrightRenderer(builder.Configuration)
    .AddStellaegisRenderingEndpoints();

app.MapGroup("/api").MapRenderingEndpoints().RequireAuthorization("rendering");
```

## [1.0.1] — 2026-10-14

Dependencies raised; nothing in the API changes.

### Changed

- Stellaegis.Primitives 2.0.0. Rendering uses none of what it breaks.
- Scriban 7.5.0: tighter loop and output limits on the templates it renders, which come from the
  caller in the remote setup.
- Microsoft.Playwright 1.63.0, driving Chromium 153: install Chromium again on the hosts that print.
- Microsoft.Extensions 10.0.13.

## [1.0.0] — 2026-10-13

First release as a standalone library.

### Added

- `ITemplateRenderer`, which renders a template and its JSON data to HTML or to PDF, and extensions
  that render any object, serialized with snake_case property names.
- The in-process renderer, `AddStellaegisRendering`, over the engine interfaces `IHtmlRenderer` and
  `IPdfRenderer`. Without a PDF engine registered, HTML still renders.
- `Stellaegis.Rendering.Scriban`: the HTML engine. Values are inserted without HTML escaping, and
  numbers keep the value they were written with.
- `Stellaegis.Rendering.Playwright`: the PDF engine. Chromium runs without JavaScript, without network
  access and in its sandbox unless the host's `PlaywrightRendererOptions` say otherwise.
- `Stellaegis.Rendering.AspNetCore`: `MapRenderingEndpoints`, behind an authorization policy, and
  `MapRenderingEndpointsAnonymous`.
- `Stellaegis.Rendering.Client`: a renderer that calls those endpoints, one call per rendering, PDF
  included.
- `TemplateException`, raised in both setups when the template is at fault.

[4.0.0]: https://github.com/Stellaegis/Stellaegis.Rendering/releases/tag/v4.0.0
[1.0.1]: https://github.com/Stellaegis/Stellaegis.Rendering/releases/tag/v1.0.1
[1.0.0]: https://github.com/Stellaegis/Stellaegis.Rendering/releases/tag/v1.0.0
