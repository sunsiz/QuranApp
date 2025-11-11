# QuranBlazor Copilot Instructions

## Project Overview
This is a .NET MAUI Blazor Hybrid application for Quran study, targeting Android, iOS, (Only Android and iOS versions are and will publish) MacCatalyst, and Windows platforms. The app provides Uzbek translations (Cyrillic and Latin scripts) alongside Arabic text with bookmarking, note-taking, and search capabilities.

## Architecture

### Technology Stack
- **.NET 9.0 MAUI** with Blazor Hybrid (Razor components)
- **SQLite** for local database (`sqlite-net-pcl` + `SQLitePCLRaw.bundle_green`)
- **CommunityToolkit.Maui** for native UI dialogs and toasts
- Multi-platform targeting: `net9.0-android`, `net9.0-ios`, `net9.0-maccatalyst`, `net9.0-windows10.0.26100.0`

### Project Structure
- **Pages/**: Blazor Razor components (`Suras.razor`, `AyaList.razor`, `Favorites.razor`, `Notes.razor`, `Search.razor`, `Settings.razor`)
- **Data/**: Domain models (`Sura.cs`, `Aya.cs`, `Note.cs`) and data access (`DbContext.cs`)
- **Services/**: Business logic (`TransliterationService.cs`, `DialogService.cs`)
- **Platforms/**: Platform-specific code for Android, iOS, MacCatalyst, Windows, Tizen
- **wwwroot/**: Static web assets (CSS, JavaScript, Bootstrap)

### Database Pattern
- SQLite database (`quran.db`) copied to `FileSystem.AppDataDirectory` on first launch
- On Windows, database is read from `AppContext.BaseDirectory`; on mobile platforms, uses `FileSystem.OpenAppPackageFileAsync`
- Data models use `[PrimaryKey, AutoIncrement]` and `[Collation("NOCASE")]` attributes from SQLite
- DbContext registered as scoped service with path injection: `AddScoped<DbContext>(s => ActivatorUtilities.CreateInstance<DbContext>(s, dbPath))`
- All database operations are async (`GetSuraListAsync()`, `GetAyaListAsync()`, `UpdateAyaAsync()`)

## Critical Developer Patterns

### Uzbek Script Handling
The app dynamically supports **Cyrillic ↔ Latin transliteration** via `TransliterationService`:

```csharp
// Always wrap UI text for display
TransliterationService.GetDisplayText("Сура Излаш")

// Check current script preference
bool isLatin = TransliterationService.IsLatinScriptSelected;

// Manual conversion when needed
string latin = TransliterationService.ToLatin(cyrillicText);
string cyrillic = TransliterationService.ToCyrillic(latinText);
```

**Script preference** is stored via `Preferences.Get/Set("UzbekScript", "Cyrillic"|"Latin")` and detected from device culture on first launch (`uz-Latn` → Latin, `uz-Cyrl` → Cyrillic). Script changes trigger `TransliterationService.OnScriptChanged` event for component updates.

### Preferences Pattern
MAUI Preferences API is used throughout for settings persistence:

```csharp
// Font sizes
Preferences.Get("ArabicFontSize", 16)
Preferences.Get("TranslateFontSize", 14)
Preferences.Get("CommentFontSize", 12)

// Last reading position
Preferences.Set("Bookmark", "49:11") // Format: "SuraId:AyaId"
```

### Scroll Position Management
Complex JavaScript interop pattern in `wwwroot/js/site.js`:

- `saveScrollPosition()`: Stores scroll position in `sessionStorage` per URL path
- `restoreScrollPosition()`: Restores on page load
- `getTopVisibleAyaIdForAyaList()`: Calculates top-visible Aya for AyaList page
- `BlazorScrollToId(id)`: Scrolls to specific element (used for Aya navigation)

**Key Razor pattern**: Pages like `Suras.razor` attach click handlers to save scroll position before navigation:

```csharp
await JsRuntime.InvokeVoidAsync("eval", @"
    document.querySelectorAll('a').forEach(anchor => {
        anchor.addEventListener('click', (e) => {
            saveScrollPosition();
        });
    });
");
```

### Dialog Service Pattern
Native dialogs accessed via `DialogService` (wraps MAUI's `Application.Current.MainPage.DisplayAlert/DisplayPrompt`):

```csharp
await Dialog.DisplayConfirm(title, message, accept, cancel);
await Dialog.DisplayPrompt(title, message, accept, cancel);
Dialog.DisplayToast(message); // CommunityToolkit.Maui.Alerts.Toast
```

Always wrap strings with `TransliterationService.GetDisplayText()` for localization.

### Navigation State Management
`AyaList.razor` implements complex navigation tracking:

- `preventScrollToFragment` flag prevents unwanted scrolling during favorites/bookmark updates
- `RegisterLocationChangingHandler()` for pre-navigation logic (replaces deprecated `OnBeforeInternalNavigation`)
- `_previousFragment` tracks URL fragments for scroll restoration
- Query parameters via `[SupplyParameterFromQuery]`: `?SuraId=2&SuraName=Al-Baqarah`

## Development Workflows

### Building
```powershell
# Restore dependencies
dotnet restore QuranBlazor.sln

# Build for specific platform
dotnet build -f net9.0-android
dotnet build -f net9.0-windows10.0.26100.0

# Debug mode enables Blazor WebView Developer Tools
```

### Running/Debugging
- Android: `EmbedAssembliesIntoApk=True` in Debug for faster deployment
- Windows: Use F5 in Visual Studio with `net9.0-windows10.0.26100.0` target
- Database tracing enabled in DEBUG builds via `_conn.Tracer` and `_conn.Trace = true`

### Database Changes
- Modify models in `Data/` folder with SQLite attributes
- Database schema must be updated manually in `quran.db` file (no migrations)
- Delete app data to trigger fresh database copy for testing

## Key Conventions

### Component Injection
Standard pattern across Razor pages:

```csharp
@inject DbContext db
@inject IJSRuntime JsRuntime
@inject NavigationManager NavigationManager
@inject DialogService Dialog
```

### Async Data Loading
Pages load data in `OnParametersSetAsync()` with loading flags:

```csharp
private bool isDataLoaded;
private bool isLoading = true;

protected override async Task OnParametersSetAsync() {
    if (!isDataLoaded) {
        await LoadDataAsync();
        isDataLoaded = true;
    }
}
```

### CSS Styling
- Bootstrap 5 classes used throughout
- Custom styles in `wwwroot/css/app.css` and component-specific `.razor.css`
- Dynamic font sizing via inline styles: `style="@textStyle"` where `textStyle = $"font-size:{Preferences.Get("TranslateFontSize", 14)}px"`
- Arabic text uses `.arabic` class
- Cards use `.bg-light.bg-gradient.rounded-pill` for consistent UI

### Platform-Specific Code
Use preprocessor directives for platform differences:

```csharp
#if WINDOWS
    sourceStream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, filename));
#else
    sourceStream = await FileSystem.Current.OpenAppPackageFileAsync(filename);
#endif
```

## Common Pitfalls

1. **Always call `TransliterationService.GetDisplayText()`** for user-facing Cyrillic text, including toast messages
2. **Use `.ConfigureAwait(false)`** when awaiting in Blazor code blocks to avoid deadlocks
3. **Set `preventScrollToFragment = true`** before state-changing operations in `AyaList.razor` to prevent unwanted scrolling
4. **Check for null** when accessing `Application.Current?.MainPage` in DialogService
5. **Query parameters require `[SupplyParameterFromQuery]`** attribute on properties
6. **JavaScript interop** requires `IJSRuntime` injection and `InvokeVoidAsync/InvokeAsync` methods
7. **Database path** must use `FileSystem.AppDataDirectory` for writable storage on mobile

## Testing Notes
- No automated test framework currently configured
- Manual testing required across Android, iOS, Windows platforms
- Test script switching thoroughly (Cyrillic ↔ Latin) as it affects search, display, and database queries
- Verify scroll position restoration after navigation (key UX feature)
