# CSV to SQLite Converter Tool

Automated tool for converting Quran translation CSV exports to SQLite database format with validation and versioning.

## Features

- ✅ Automatic CSV parsing with error handling
- ✅ Data cleanup (trim whitespace, normalize text)
- ✅ Skip empty/invalid rows
- ✅ Generate SHA256 checksums
- ✅ Create version manifest JSON
- ✅ Batch insert optimization
- ✅ Progress reporting

## Usage

### Build the Tool

```bash
cd Tools/CsvToSqlite
dotnet build -c Release
```

### Run Conversion

```bash
# Basic usage
dotnet run --project Tools/CsvToSqlite/CsvToSqlite.csproj <input.csv> <output.db> [version]

# Example
dotnet run --project Tools/CsvToSqlite/CsvToSqlite.csproj Uzbek_Meal20251111.csv quran.db 2025.11.11
```

Or use the compiled executable:

```bash
cd Tools/CsvToSqlite/bin/Release/net9.0
./CsvToSqlite.exe "C:\path\to\Uzbek_Meal20251111.csv" "quran.db" "2025.11.11"
```

### Output

The tool generates:
1. `quran.db` - SQLite database file
2. `quran.json` - Manifest file with version info and checksum

Example manifest:
```json
{
  "version": "2025.11.11",
  "date": "2025-11-11T10:30:00Z",
  "fileName": "quran.db",
  "size": 2457600,
  "sha256": "a3f5e8c9d2b4...",
  "suraCount": 114,
  "ayaCount": 6236,
  "minAppVersion": "1.0.0"
}
```

## CSV Format

Expected columns:
- `SureNo` - Sura number (1-114)
- `SureName` - Sura name in Uzbek
- `AyahNo` - Aya number
- `Arabic` - Arabic text
- `Text` - Uzbek translation
- `Comment` - Brief commentary (optional)
- `DetailComment` - Detailed commentary (optional)

## Data Cleanup

The tool automatically:
- Trims leading/trailing whitespace
- Removes multiple consecutive spaces
- Normalizes line breaks
- Skips completely empty rows
- Handles missing optional fields

## Workflow

1. **Export from SQL Server**
   - Connect via SSMS
   - Export to Excel
   - Save as CSV (UTF-8)

2. **Convert to SQLite**
   ```bash
   dotnet run Uzbek_Meal20251111.csv quran.db 2025.11.11
   ```

3. **Upload Files**
   - Upload `quran.db` to hosting/CDN
   - Upload `quran.json` manifest
   - Update app to point to new version

## Publishing Database Updates

### Option 1: GitHub Releases
```bash
# Tag release
git tag -a v2025.11.11 -m "Translation update November 2025"
git push origin v2025.11.11

# Upload as release assets:
# - quran.db
# - quran.json
```

### Option 2: Direct Hosting
Upload both files to your web server:
```
https://yoursite.com/quran/v2025.11.11/quran.db
https://yoursite.com/quran/v2025.11.11/quran.json
```

Or use a CDN:
```
https://cdn.yoursite.com/quran/latest/quran.db
https://cdn.yoursite.com/quran/latest/quran.json
```

## Troubleshooting

### Error: "CSV file not found"
- Check file path is correct
- Use absolute paths or ensure working directory is correct

### Error: "Bad data on line X"
- Open CSV in text editor to inspect line X
- Remove or fix problematic characters
- Ensure CSV is UTF-8 encoded

### Missing Data
- Check column names match exactly
- Verify CSV has header row
- Ensure all required columns present
