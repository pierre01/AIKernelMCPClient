# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade Lights.Common\Lights.Common.csproj
4. Upgrade Lights.WPFHouse\Lights.WpfHouse.csproj
5. Upgrade Lights.RestApi\Lights.RestApi.csproj
6. Upgrade Lights.McpServer\Lights.McpServer.csproj
7. Upgrade Lights.MauiClient\Lights.MauiClient.csproj


## Settings

This section contains settings and data used by execution steps.

### Excluded projects

Table below contains projects that do belong to the dependency graph for selected projects and should not be included in the upgrade.

| Project name                                   | Description                 |
|:-----------------------------------------------|:---------------------------:|


### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                    | Current Version                         | New Version  | Description                                   |
|:------------------------------------------------|:---------------------------------------:|:------------:|:----------------------------------------------|
| Microsoft.AspNetCore.OpenApi                    | 9.0.9                                   | 10.0.0       | Replace with Microsoft.AspNetCore.OpenApi 10.0.0 for .NET 10.0 |
| Microsoft.Extensions.ApiDescription.Server      | 9.0.9                                   | 10.0.0       | Replace with Microsoft.Extensions.ApiDescription.Server 10.0.0 for .NET 10.0 |
| Microsoft.Extensions.Logging.Debug              | 10.0.0-preview.1.25080.5                | 10.0.0       | Update pre-release logging package to stable 10.0.0 |


### Project upgrade details
This section contains details about each project upgrade and modifications that need to be done in the project.

#### Lights.Common\Lights.Common.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - None detected for this project in analysis results.

Feature upgrades:
  - None identified.

Other changes:
  - Ensure code compiles against .NET 10.0 and address any API breaking changes reported by the compiler.


#### Lights.WPFHouse\Lights.WpfHouse.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0-windows` to `net10.0-windows`

NuGet packages changes:
  - None detected for this project in analysis results.

Feature upgrades:
  - Verify any Windows-specific APIs and SDK requirements for .NET 10.0-windows.

Other changes:
  - Ensure the project's RuntimeIdentifier/TargetFramework settings align with .NET 10.0-windows requirements.


#### Lights.RestApi\Lights.RestApi.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - `Microsoft.AspNetCore.OpenApi` should be updated from `9.0.9` to `10.0.0`.
  - `Microsoft.Extensions.ApiDescription.Server` should be updated from `9.0.9` to `10.0.0`.

Feature upgrades:
  - Review OpenAPI/Swagger integration for any package API changes in the 10.0 packages.

Other changes:
  - Run the project build and fix any compilation or runtime issues introduced by package or framework changes.


#### Lights.McpServer\Lights.McpServer.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - None detected for this project in analysis results.

Feature upgrades:
  - None identified.

Other changes:
  - Ensure server project builds and tests (if any) pass under .NET 10.0.


#### Lights.MauiClient\Lights.MauiClient.csproj modifications

Project properties changes:
  - Project currently targets `net9.0-android;net9.0-ios;net9.0-maccatalyst;net9.0-windows10.0.19041.0`.
  - Add `net10.0-windows` to the target frameworks so it becomes: `net9.0-android;net9.0-ios;net9.0-maccatalyst;net9.0-windows10.0.19041.0;net10.0-windows`

NuGet packages changes:
  - `Microsoft.Extensions.Logging.Debug` should be updated from `10.0.0-preview.1.25080.5` to `10.0.0`.

Feature upgrades:
  - Verify .NET MAUI compatibility with .NET 10.0 for the platforms targeted. Address any breaking changes noted in MAUI dependencies.

Other changes:
  - Validate MAUI resource dictionaries and platform-specific settings after the target framework update.


