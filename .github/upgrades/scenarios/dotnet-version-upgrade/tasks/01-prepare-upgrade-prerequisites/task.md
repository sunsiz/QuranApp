# 01-prepare-upgrade-prerequisites: Validate SDK and repo upgrade prerequisites

Confirm the repository is ready for the framework move by validating .NET 10 SDK compatibility against global.json, confirming restore/build prerequisites, and capturing any prerequisite adjustments required before project edits.

This task exists to ensure upgrade edits happen in a stable environment and to avoid partial migrations caused by missing SDK/tooling prerequisites.

## Scope Inventory
- **Projects affected**: `QuranBlazor/QuranBlazor.csproj` (single SDK-style MAUI project).
- **Distinct concerns**: SDK availability, global.json compatibility behavior, baseline build prerequisites for the solution.
- **Change signals**: Assessment indicates low upgrade difficulty with no API incompatibilities and only package/TFM upgrade recommendations.
- **Skill matches**: `building-projects` for validation command strategy and `managing-target-frameworks` for upcoming TFM transition consistency.

## Research Findings
- `validate_dotnet_sdk_installation(net10.0)` reports a compatible .NET SDK is installed.
- `validate_dotnet_sdk_in_globaljson(...)` reports roll-forward compatibility guidance for .NET 10 upgrades.
- Repository `global.json` currently pins SDK `9.0.307` with `rollForward: latestFeature`; this should be updated to align with cross-major upgrade expectations before project retargeting.
- Build approach for this repository remains `dotnet build` (SDK-style MAUI project).

**Done when**: .NET 10 SDK compatibility is validated for this repository and prerequisite blockers are either resolved or documented with a clear next action.
