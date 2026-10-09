# Projects and dependencies analysis

This document provides a comprehensive overview of the projects and their dependencies in the context of upgrading to .NETCoreApp,Version=v10.0.

## Table of Contents

- [Executive Summary](#executive-Summary)
  - [Highlevel Metrics](#highlevel-metrics)
  - [Projects Compatibility](#projects-compatibility)
  - [Package Compatibility](#package-compatibility)
  - [API Compatibility](#api-compatibility)
  - [Binding Redirect Configuration](#binding-redirect-configuration)
- [Aggregate NuGet packages details](#aggregate-nuget-packages-details)
- [Top API Migration Challenges](#top-api-migration-challenges)
  - [Technologies and Features](#technologies-and-features)
  - [Most Frequent API Issues](#most-frequent-api-issues)
- [Projects Relationship Graph](#projects-relationship-graph)
- [Project Details](#project-details)

  - [QuranBlazor\QuranBlazor.csproj](#quranblazorquranblazorcsproj)


## Executive Summary

### Highlevel Metrics

| Metric | Count | Status |
| :--- | :---: | :--- |
| Total Projects | 1 | All require upgrade |
| Total NuGet Packages | 10 | 3 need upgrade |
| Total Code Files | 28 |  |
| Total Code Files with Incidents | 1 |  |
| Total Lines of Code | 2604 |  |
| Total Number of Issues | 4 |  |
| Estimated LOC to modify | 0+ | at least 0.0% of codebase |

### Projects Compatibility

| Project | Target Framework | Difficulty | Package Issues | API Issues | Binding Issues | Est. LOC Impact | Description |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| [QuranBlazor\QuranBlazor.csproj](#quranblazorquranblazorcsproj) | net9.0-maccatalyst;net9.0-android;net9.0-ios;net9.0-windows10.0.26100.0 | 🟢 Low | 3 | 0 | 0 |  | DotNetCoreApp, Sdk Style = True |

### Package Compatibility

| Status | Count | Percentage |
| :--- | :---: | :---: |
| ✅ Compatible | 7 | 70.0% |
| ⚠️ Incompatible | 0 | 0.0% |
| 🔄 Upgrade Recommended | 3 | 30.0% |
| ***Total NuGet Packages*** | ***10*** | ***100%*** |

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

## Aggregate NuGet packages details

| Package | Current Version | Suggested Version | Projects | Description |
| :--- | :---: | :---: | :--- | :--- |
| CommunityToolkit.Maui | 12.3.0 |  | [QuranBlazor.csproj](#quranblazorquranblazorcsproj) | ✅Compatible |
| Microsoft.AspNetCore.Components.WebView.Maui | 9.0.120 |  | [QuranBlazor.csproj](#quranblazorquranblazorcsproj) | ✅Compatible |
| Microsoft.AspNetCore.WebUtilities | 9.0.10 | 10.0.12 | [QuranBlazor.csproj](#quranblazorquranblazorcsproj) | NuGet package upgrade is recommended |
| Microsoft.Data.Sqlite.Core | 9.0.10 | 10.0.12 | [QuranBlazor.csproj](#quranblazorquranblazorcsproj) | NuGet package upgrade is recommended |
| Microsoft.Extensions.Logging.Debug | 9.0.10 | 10.0.12 | [QuranBlazor.csproj](#quranblazorquranblazorcsproj) | NuGet package upgrade is recommended |
| Microsoft.Maui.Controls | 9.0.120 |  | [QuranBlazor.csproj](#quranblazorquranblazorcsproj) | ✅Compatible |
| Microsoft.Maui.Controls.Compatibility | 9.0.120 |  | [QuranBlazor.csproj](#quranblazorquranblazorcsproj) | ✅Compatible |
| sqlite-net-pcl | 1.9.172 |  | [QuranBlazor.csproj](#quranblazorquranblazorcsproj) | ✅Compatible |
| SQLitePCLRaw.bundle_green | 2.1.11 |  | [QuranBlazor.csproj](#quranblazorquranblazorcsproj) | ✅Compatible |
| SQLitePCLRaw.provider.dynamic_cdecl | 3.0.2 |  | [QuranBlazor.csproj](#quranblazorquranblazorcsproj) | ✅Compatible |

## Top API Migration Challenges

### Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |

### Most Frequent API Issues

| API | Count | Percentage | Category |
| :--- | :---: | :---: | :--- |

## Projects Relationship Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart LR
    P1["<b>📦&nbsp;QuranBlazor.csproj</b><br/><small>net9.0-maccatalyst;net9.0-android;net9.0-ios;net9.0-windows10.0.26100.0</small>"]
    click P1 "#quranblazorquranblazorcsproj"

```

## Project Details

<a id="quranblazorquranblazorcsproj"></a>
### QuranBlazor\QuranBlazor.csproj

#### Project Info

- **Current Target Framework:** net9.0-maccatalyst;net9.0-android;net9.0-ios;net9.0-windows10.0.26100.0
- **Proposed Target Framework:** net9.0-maccatalyst;net9.0-android;net9.0-ios;net9.0-windows10.0.26100.0;net10.0-windows
- **SDK-style**: True
- **Project Kind:** DotNetCoreApp
- **Dependencies**: 0
- **Dependants**: 0
- **Number of Files**: 138
- **Number of Files with Incidents**: 1
- **Lines of Code**: 2604
- **Estimated LOC to modify**: 0+ (at least 0.0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["QuranBlazor.csproj"]
        MAIN["<b>📦&nbsp;QuranBlazor.csproj</b><br/><small>net9.0-maccatalyst;net9.0-android;net9.0-ios;net9.0-windows10.0.26100.0</small>"]
        click MAIN "#quranblazorquranblazorcsproj"
    end

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 0 |  |
| ***Total APIs Analyzed*** | ***0*** |  |

