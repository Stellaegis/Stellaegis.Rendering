# Third-party notices

Stellaegis.Rendering is licensed under the MIT License (see [LICENSE](LICENSE)).

Its packages contain no third-party code. They reference the packages below, which NuGet installs
alongside them, each under its own license.

| Component | Used by | License (SPDX) | Project |
| --- | --- | --- | --- |
| Scriban | `Stellaegis.Rendering.Scriban` | BSD-2-Clause | <https://github.com/scriban/scriban> |
| Microsoft.Playwright | `Stellaegis.Rendering.Playwright` | MIT | <https://github.com/microsoft/playwright-dotnet> |
| Stellaegis.Primitives | all packages | MIT | <https://github.com/Stellaegis/Stellaegis.Primitives> |
| Microsoft.Extensions.\*, ASP.NET Core | all packages | MIT | <https://github.com/dotnet> |

Microsoft.Playwright carries the Playwright driver and a Node.js runtime, under the licenses its
package declares. The Chromium browser is not part of any package: Playwright's installation script
downloads it onto the machine that prints, under Chromium's own licenses.

Test-only dependencies (xUnit, ASP.NET Core TestHost) are not part of any
package and are not listed here.
