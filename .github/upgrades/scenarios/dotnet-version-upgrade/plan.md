# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade QuranBlazor from .NET 9 multi-target MAUI TFMs to .NET 10 equivalents and align recommended package versions.
**Scope**: Single SDK-style MAUI Blazor Hybrid project with low compatibility risk.

## Tasks

### 01-prepare-upgrade-prerequisites: Validate SDK and repo upgrade prerequisites

Confirm the repository is ready for the framework move by validating .NET 10 SDK compatibility against global.json, confirming restore/build prerequisites, and capturing any prerequisite adjustments required before project edits.

This task exists to ensure upgrade edits happen in a stable environment and to avoid partial migrations caused by missing SDK/tooling prerequisites.

**Done when**: .NET 10 SDK compatibility is validated for this repository and prerequisite blockers are either resolved or documented with a clear next action.

---

### 02-upgrade-quranblazor-project: Upgrade project TFMs, packages, and validation

Upgrade the QuranBlazor project target frameworks from net9.0-* to net10.0-* equivalents, apply recommended package version upgrades, and adjust any compatibility issues discovered during build/restore.

This task includes full validation after edits so the upgraded project restores and builds cleanly across targeted platforms, with warnings resolved in modified scope.

**Done when**: QuranBlazor targets .NET 10 for its configured platforms, recommended package updates are applied, and solution/project build validation completes without errors.
