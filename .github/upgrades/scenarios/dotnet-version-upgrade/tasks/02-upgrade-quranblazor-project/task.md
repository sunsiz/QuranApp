# 02-upgrade-quranblazor-project: Upgrade project TFMs, packages, and validation

Upgrade the QuranBlazor project target frameworks from net9.0-* to net10.0-* equivalents, apply recommended package version upgrades, and adjust any compatibility issues discovered during build/restore.

This task includes full validation after edits so the upgraded project restores and builds cleanly across targeted platforms, with warnings resolved in modified scope.

## Scope Inventory
- **Projects affected**: `QuranBlazor/QuranBlazor.csproj`.
- **Distinct concerns**: TFM retargeting across MAUI platforms, package version updates, build verification under .NET 10 SDK.
- **Change signals**: Assessment flagged project TFM change (`Project.0002`) and three package upgrades (`NuGet.0002`) with no API incompatibility issues.
- **Skill matches**: `managing-target-frameworks`, `managing-package-references`, `building-projects`.

## Research Findings
- Project currently targets `net9.0-maccatalyst;net9.0-android;net9.0-ios` plus conditional `net9.0-windows10.0.26100.0`.
- Debug/Release configuration conditions also include explicit net9.0-* TFMs and must be kept aligned after retargeting.
- Assessment-recommended package updates:
  - `Microsoft.AspNetCore.WebUtilities` → `10.0.12`
  - `Microsoft.Data.Sqlite.Core` → `10.0.12`
  - `Microsoft.Extensions.Logging.Debug` → `10.0.12`
- Vulnerability mitigation: replace `SQLitePCLRaw.bundle_green` 2.1.11 with `SQLitePCLRaw.bundle_e_sqlite3` 3.0.5 and align `SQLitePCLRaw.provider.dynamic_cdecl` to 3.0.5 to address NU1903 advisory warnings.
- Build workflow should use restore+build with `dotnet` for this SDK-style MAUI project.

**Done when**: QuranBlazor targets .NET 10 for its configured platforms, recommended package updates are applied, and solution/project build validation completes without errors.
