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

### Excluded projects

Table below contains projects that do belong to the dependency graph for selected projects and should not be included in the upgrade.

| Project name                                   | Description                 |
|:-----------------------------------------------|:---------------------------:|


### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                | Current Version                       | New Version | Description                                   |
|:--------------------------------------------|:--------------------------------------:|:-----------:|:----------------------------------------------|
| Microsoft.AspNetCore.OpenApi                | 9.0.9                                 | 10.0.0      | Update recommended for .NET 10.0               |
| Microsoft.Extensions.ApiDescription.Server  | 9.0.9                                 | 10.0.0      | Update recommended for .NET 10.0               |
| Microsoft.Extensions.Logging.Debug          | 10.0.0-preview.1.25080.5              | 10.0.0      | Preview package: update to stable 10.0.0       |

### Project upgrade details

#### Lights.Common modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - No NuGet package changes required by analysis for this project.

Other changes:
  - Verify code for any API breaking changes after target framework update.

#### Lights.WPFHouse modifications

Project properties changes:
  - Target framework should be changed from `net9.0-windows` to `net10.0-windows`

NuGet packages changes:
  - No NuGet package changes required by analysis for this project.

Other changes:
  - Verify WPF-specific APIs and Windows SDK targeting after upgrade.

#### Lights.RestApi modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - `Microsoft.AspNetCore.OpenApi` should be updated from `9.0.9` to `10.0.0`.
  - `Microsoft.Extensions.ApiDescription.Server` should be updated from `9.0.9` to `10.0.0`.

Other changes:
  - Verify OpenAPI and API description integration after package updates.

#### Lights.McpServer modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - No NuGet package changes required by analysis for this project.

Other changes:
  - Verify any runtime or hosting changes for Kestrel and middleware.

#### Lights.MauiClient modifications

Project properties changes:
  - Target frameworks should be changed from `net9.0-android;net9.0-ios;net9.0-maccatalyst;net9.0-windows10.0.19041.0` to `net9.0-android;net9.0-ios;net9.0-maccatalyst;net9.0-windows10.0.19041.0;net10.0-windows` (add `net10.0-windows` target)

NuGet packages changes:
  - `Microsoft.Extensions.Logging.Debug` should be updated from `10.0.0-preview.1.25080.5` to `10.0.0`.

Other changes:
  - Verify MAUI platform support for `net10.0-windows` and fix any AOT or platform-specific issues.
