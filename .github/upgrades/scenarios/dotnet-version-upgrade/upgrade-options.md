# Upgrade Options — QuranBlazor

Assessment: 1 project on modern .NET (net9.0 multi-target MAUI), low complexity, no API compatibility incidents.

## Strategy

### Upgrade Strategy
A single modern .NET project with no dependency graph is best handled in one atomic pass.

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | Upgrade the full project in one pass, then validate end-to-end. |
| Top-Down | Upgrade entry-point apps first while keeping shared components incrementally buildable. |
