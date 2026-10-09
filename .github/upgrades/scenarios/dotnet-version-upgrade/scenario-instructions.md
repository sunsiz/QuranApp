# .NET Version Upgrade

## Strategy
All-at-Once

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0
- **Upgrade Scope**: Solution
- **Commit Strategy**: After Each Task

## Upgrade Options
**Source**: .github/upgrades/scenarios/dotnet-version-upgrade/upgrade-options.md

### Strategy
- Upgrade Strategy: All-at-Once

## Source Control
- **Source Branch**: master
- **Working Branch**: upgrade-dotnet-10
- **Commit Strategy**: After Each Task
- **Branch Sync**: Auto (Merge)

## Decisions
- Use an all-at-once upgrade strategy because the solution has a single modern .NET SDK-style project.

## Custom Instructions
