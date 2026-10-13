# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[1.0.0]: https://github.com/Stellaegis/Stellaegis.Rendering/releases/tag/v1.0.0
