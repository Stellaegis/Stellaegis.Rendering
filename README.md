# Stellaegis.Rendering

Stellaegis.Rendering is a .NET library that renders a template and its data to HTML or to PDF — an
invoice, a report, the body of an email. Your code depends on one interface, `ITemplateRenderer`.
The adapter you register decides where the rendering happens: in your own process, or on a remote
host reached over HTTP. A template renders the same way in both cases, so moving from one setup to
the other changes the registration at startup and nothing else.

Templates are written in [Scriban](https://github.com/scriban/scriban), a text templating language.
PDFs are printed by Chromium, driven by [Playwright](https://playwright.dev/dotnet/).

The library targets .NET 10 and is released under the MIT license.

## Packages

The library is split so that you only take the dependencies you use. The PDF engine, in particular,
requires a Chromium browser installed on the machine; a process that only renders HTML does not
need it.

| Package | Contents | Depends on |
| --- | --- | --- |
| `Stellaegis.Rendering` | `ITemplateRenderer`, the engine interfaces `IHtmlRenderer` and `IPdfRenderer`, `PdfOptions`, and the in-process renderer | `Stellaegis.Primitives`, Microsoft.Extensions abstractions |
| `Stellaegis.Rendering.Scriban` | The HTML engine: renders Scriban templates | Scriban |
| `Stellaegis.Rendering.Playwright` | The PDF engine: prints HTML with Chromium | Microsoft.Playwright |
| `Stellaegis.Rendering.AspNetCore` | Endpoints that expose a renderer over HTTP | ASP.NET Core |
| `Stellaegis.Rendering.Client` | A renderer that calls those endpoints from another process | `Microsoft.Extensions.Http` |

Each engine package is named after the technology it uses. Another PDF engine could be added as a
package of its own, next to `.Playwright`, without changing the interfaces.

## Rendering in your process

Register the in-process renderer and the engines it uses:

```csharp
builder.Services.AddStellaegisRendering()
    .AddStellaegisScribanRenderer()
    .AddStellaegisPlaywrightRenderer(builder.Configuration);
```

Then inject `ITemplateRenderer` where you need it:

```csharp
public sealed class InvoicePrinter(ITemplateRenderer renderer)
{
    public Task<byte[]> PrintAsync(string template, Invoice invoice, CancellationToken ct) =>
        renderer.RenderPdfAsync(template, invoice, new PdfOptions { Format = "A4" }, ct);
}
```

The PDF engine is optional. Without `AddStellaegisPlaywrightRenderer`, HTML renders normally and
`RenderPdfAsync` throws an `InvalidOperationException` that names the package to add. A service that
only sends emails, for instance, registers the HTML engine alone and never installs Chromium.

The PDF engine declares the module's capability `RenderingCapabilities.Pdf`. An application that
needs PDF says so, and then fails at startup, rather than on its first PDF, when no engine holds it:

```csharp
builder.Services.AddStellaegisModuleParameter<RenderingModule>(RenderingCapabilities.Pdf);
```

An ASP.NET Core host checks it by itself at startup; an application without ASP.NET Core calls
`ValidateStellaegisModules()` once its services are built (`Stellaegis.Primitives.DependencyInjection`).

## Rendering on a remote host

One process hosts the engines and exposes them over HTTP. Other processes render through a client
that implements the same `ITemplateRenderer`. This keeps Chromium on a single machine, at the cost of
one network call per rendering and of a host to secure.

On the host, register the endpoints beside the engines, map them under a route group, and attach
your security to the group:

```csharp
builder.Services.AddStellaegisRendering()
    .AddStellaegisScribanRenderer()
    .AddStellaegisPlaywrightRenderer(builder.Configuration)
    .AddStellaegisRenderingEndpoints();
builder.Services.AddAuthentication(/* your scheme */);
builder.Services.AddAuthorization(o => o.AddPolicy("rendering", p => p.RequireAuthenticatedUser()));

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapGroup("/api").MapRenderingEndpoints().RequireAuthorization("rendering");
```

Two endpoints are mapped under the group, each labelled with its operation from
`RenderingOperations`, both of kind `Read`. The PDF route is mapped only when a PDF engine is
registered: a host without one has no such route.

| Route | Operation |
| --- | --- |
| `POST rendering/html` | `templates.render_html` |
| `POST rendering/pdf` | `templates.render_pdf` |

A host with a rights system of its own protects them apart, one policy per operation:
`.RequireAuthorizationPerOperation(op => op.ToString())`.

Each request carries its own template, and printing a PDF runs a browser on the host. The endpoints
are therefore closed until you declare their authorization: a host that maps them without
`RequireAuthorization(...)`, `RequireAuthorizationPerOperation(...)` or, on a host that only a trusted
network can reach, `AllowAnonymous()`, fails at startup. They load no resource, so they ask for no
authorization handler. Everything else about the host is yours to configure: the authentication
scheme, CORS, rate limiting, forwarded headers, health checks.

In the calling process, register the client and attach the authentication your host expects, as a
`DelegatingHandler`:

```csharp
builder.Services.AddStellaegisRenderingClient(builder.Configuration)
    .AddHttpMessageHandler<MyTokenHandler>();
```

```json
{
  "Stellaegis": {
    "Rendering": {
      "Client": { "BaseUrl": "https://rendering.example.com/api/rendering/", "Timeout": "00:01:00" }
    }
  }
}
```

A PDF is rendered in a single call: the client sends the template and its data, and receives the
PDF. The intermediate HTML never travels over the network, which matters for large documents.

## The data a template receives

The data is a JSON object. Each of its properties becomes a variable of the template, under its JSON
name, unchanged.

```csharp
var html = await renderer.RenderHtmlAsync(
    "<p>{{ customerName }}</p>",
    JsonDocument.Parse("""{"customerName":"Dupont"}""").RootElement);
```

You can also pass any object. It is serialized to JSON first, with property names in snake_case:
`CustomerName` becomes `customer_name`. Dictionary keys are kept as they are.

```csharp
var html = await renderer.RenderHtmlAsync(
    "<p>{{ customer_name }}, {{ issued_at | date.parse | date.to_string '%d/%m/%Y' }}</p>",
    new { CustomerName = "Dupont", IssuedAt = new DateTime(2026, 10, 13) });
// <p>Dupont, 13/10/2026</p>
```

The data goes through JSON in both setups, including in process. This costs one serialization, which
is small next to a rendering, and it is what makes a template behave the same in process and behind
the API. The consequence is that values arrive as JSON carries them:

- **dates are strings** in ISO 8601 format. Convert them with `date.parse` before formatting them;
- **numbers keep the value they were written with.** Integers are integers, and other numbers are
  `decimal`, so `0.1 + 0.2` prints `0.3` and an amount is never changed by binary floating point.
  Only a number beyond the range of `decimal` becomes a `double`.

## Values are inserted as they are

The HTML engine inserts values without escaping them. A value that contains markup becomes markup in
the output. This lets your data carry formatted content — a rich-text paragraph, a block prepared by
the caller — and it also means that a value typed by a user can inject HTML into the document.

Escape the values that come from users with Scriban's `html.escape`:

```text
<p>{{ comment | html.escape }}</p>
```

## Printing PDFs with Chromium

The PDF engine needs Chromium on the machine that prints. Playwright places a script in your
application's output directory that installs it; on Linux, `--with-deps` also installs the system
libraries Chromium needs:

```sh
pwsh bin/Release/net10.0/playwright.ps1 install --with-deps chromium
```

Chromium runs the HTML it prints, on the machine that prints it. A template could otherwise make that
machine call an address of your internal network, and print the answer into the PDF. On a remote
host, the template comes from the caller, so this is the caller's reach into your network. By
default, Chromium is therefore confined:

| Setting | Default | Effect |
| --- | --- | --- |
| `JavaScriptEnabled` | `false` | Scripts in the printed HTML do not run. |
| `NetworkEnabled` | `false` | Every request the page makes is refused: images, fonts and styles must be embedded in the HTML, inline or as `data:` URLs. |
| `Sandbox` | `true` | Chromium runs in its sandbox. See below for the machines where the sandbox cannot start. |

These settings are read from the section `Stellaegis:Rendering:Playwright`. They belong to the host:
a request cannot change them, since a request able to open the network would undo the confinement.
`PdfOptions`, which travels with each request, only describes the page — its format, its margins,
and whether backgrounds are printed.

```json
{
  "Stellaegis": {
    "Rendering": {
      "Playwright": { "JavaScriptEnabled": false, "NetworkEnabled": false, "Sandbox": true }
    }
  }
}
```

Chromium's sandbox cannot start on two kinds of Linux machine, and printing then fails with
`No usable sandbox!`:

- **a container running as root.** Run the container as another user, or set `Sandbox` to `false`;
- **Ubuntu 23.10 and later**, and other distributions where AppArmor restricts unprivileged user
  namespaces, which the sandbox needs. Allow them for Chromium — the
  [Chromium documentation](https://chromium.googlesource.com/chromium/src/+/main/docs/security/apparmor-userns-restrictions.md)
  describes how —, or set `Sandbox` to `false`.

Setting `Sandbox` to `false` keeps the two other protections, JavaScript and network, and removes the
isolation of Chromium from the rest of the machine. The choice is the host's to make.

## Errors

The same call fails the same way in both setups:

| Exception | Cause |
| --- | --- |
| `ArgumentException` | A blank template, or data that is not a JSON object. The client checks it before sending; a caller that is not the client gets a `400` problem, whose detail is shown in `Development` only. |
| `TemplateException` | The template does not parse, or fails while rendering. Fixing it means changing the template; retrying does not help. Over HTTP, the endpoints answer a `400` problem of type `urn:stellaegis:rendering:template-error`, which the client raises again as `TemplateException`. |
| `HttpRequestException` | Remote setup only: the host could not be reached, or refused the call. The message keeps what the host answered. |

## Building and testing

```sh
dotnet test --solution Stellaegis.Rendering.slnx
```

The same test cases run against the in-process renderer and against the client calling the endpoints
in an in-memory host. The PDF tests, and the tests of Chromium's confinement, need Playwright's
Chromium: install it with the script found in the test project's output directory,
`tests/Stellaegis.Rendering.Tests/bin/Debug/net10.0/playwright.ps1`. Without Chromium, these tests are
skipped and the others still run.

## License

MIT — see [LICENSE](https://github.com/Stellaegis/Stellaegis.Rendering/blob/main/LICENSE). The packages
depend on third-party components listed in
[THIRD-PARTY-NOTICES.md](https://github.com/Stellaegis/Stellaegis.Rendering/blob/main/THIRD-PARTY-NOTICES.md).
