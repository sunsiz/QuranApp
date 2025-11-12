# CSV to SQLite Converter for QuranBlazor - UPDATED

## ⚠️ CRITICAL CHANGE: User Data Preservation

**Version 2.0** - This tool now **updates translations only** while preserving all database structure and user data.

### What Changed?

**OLD Behavior (WRONG ❌):**
- Deleted existing database
- Created new database from scratch
- Set all Aya Ids to 0 (AutoIncrement)
- Lost all user data

**NEW Behavior (CORRECT ✅):**
- Copies template database as base
- Updates ONLY Aya translation fields
- Preserves Aya Ids (AutoIncrement primary keys)
- Preserves Sura table metadata
- Preserves database structure
- User data merged during app update process

## Usage

### New Command Format

```bash
dotnet run <input.csv> <template.db> <output.db> [version]
```

### Parameters

1. **input.csv** - CSV file with updated translations from SQL Server
2. **template.db** - Your existing bundled quran.db (from Resources/Raw/)
3. **output.db** - Output database for deployment
4. **version** - (Optional) Version in YYYY.MM.DD format

### Example

```bash
cd Tools/CsvToSqlite

# Use your app's bundled database as template
dotnet run Uzbek_Meal20251111.csv ..\..\QuranBlazor\Resources\Raw\quran.db quran_update.db 2025.11.11
```

## How It Works

### Step-by-Step Process

1. **Copy Template Database**
   ```
   Template (quran.db) → Output (quran_update.db)
   - Preserves all 114 Suras with metadata
   - Preserves all 6236 Ayas with correct Ids
   - Preserves table schema
   ```

2. **Parse CSV File**
   ```
   Read translations from CSV:
   - SureNo, AyahNo → Used to find matching Aya
   - Text, Arabic, Comment, DetailComment → Update fields
   ```

3. **Update Translations**
   ```sql
   For each CSV row:
     Find Aya WHERE SuraId = SureNo AND AyaId = AyahNo
     UPDATE Aya SET 
       Text = <new translation>,
       Arabic = <new arabic>,
       Comment = <new comment>,
       DetailComment = <new detail>
     WHERE Id = <existing Aya.Id>  -- Preserves AutoIncrement Id!
   ```

4. **Preserve User Data Flags**
   ```
   NOT updated (preserved):
   - Aya.Id (AutoIncrement primary key)
   - Aya.IsFavorite (user's bookmark status)
   - Aya.HasNote (user's note existence)
   ```

5. **Generate Checksum & Manifest**
   ```
   - Calculate SHA256 of output database
   - Create quran.json with metadata
   ```

## CSV Format

Required columns (order matters):

| Column        | Type   | Purpose                         |
|---------------|--------|---------------------------------|
| SureNo        | int    | Match Aya.SuraId                |
| SureName      | string | (Not used, metadata preserved)  |
| AyahNo        | int    | Match Aya.AyaId                 |
| Arabic        | string | Update Aya.Arabic               |
| Text          | string | Update Aya.Text                 |
| Comment       | string | Update Aya.Comment              |
| DetailComment | string | Update Aya.DetailComment        |

## User Data Protection

### What Happens to User Data?

**During Conversion (CSV → Database):**
- ❌ User notes NOT included (belong to individual users)
- ❌ Bookmark collections NOT included (belong to individual users)
- ✅ Base Quran structure preserved
- ✅ Translation fields updated

**During App Update (Download → Install):**
The `TranslationUpdateService.MergeTranslations()` method:
1. Downloads new database to temp location
2. Opens user's existing database
3. **Merges only Aya translations** from downloaded DB
4. **Preserves all user data** in existing DB
5. User notes, bookmarks, collections remain intact

### Merge Process Diagram

```
Downloaded DB (quran_update.db)        User's DB (on device)
┌─────────────────────┐                ┌─────────────────────┐
│ Aya Table           │                │ Aya Table           │
│ - Id: 1             │                │ - Id: 1             │
│ - SuraId: 1         │                │ - SuraId: 1         │
│ - AyaId: 1          │                │ - AyaId: 1          │
│ - Text: "NEW"       │  ────────────► │ - Text: "NEW" ✅    │
│ - Arabic: "NEW"     │  Update only   │ - Arabic: "NEW" ✅  │
│ - IsFavorite: false │  translation   │ - IsFavorite: true  │ Preserved ✅
│ - HasNote: false    │  fields        │ - HasNote: true     │ Preserved ✅
└─────────────────────┘                └─────────────────────┘

                                       ┌─────────────────────┐
                                       │ Note Table          │
                                       │ - Id: 1             │
                                       │ - SuraId: 1         │
                                       │ - AyaId: 1          │ Preserved ✅
                                       │ - Title: "..."      │ Preserved ✅
                                       │ - Content: "..."    │ Preserved ✅
                                       └─────────────────────┘
```

## Validation

After conversion, verify:

```bash
# Check database integrity
sqlite3 quran_update.db "SELECT COUNT(*) FROM Sura"
# Expected: 114

sqlite3 quran_update.db "SELECT COUNT(*) FROM Aya"
# Expected: 6236

sqlite3 quran_update.db "SELECT MIN(Id), MAX(Id) FROM Aya"
# Expected: 1, 6236 (or similar sequential range)

# Check that Ids are NOT all 0
sqlite3 quran_update.db "SELECT COUNT(*) FROM Aya WHERE Id = 0"
# Expected: 0

# Verify translations updated
sqlite3 quran_update.db "SELECT Text FROM Aya WHERE SuraId = 1 AND AyaId = 1"
# Expected: Your new translation text
```

## Common Issues

### Issue: All Aya Ids are 0

**Cause:** Used old version of converter that created new database instead of updating template.

**Fix:** 
1. Get latest converter: `git pull` or download updated Program.cs
2. Use 3-parameter format: `dotnet run csv template.db output.db version`
3. Verify template.db has correct Aya Ids before conversion

### Issue: User data lost after update

**Cause:** App is replacing database file instead of merging translations.

**Fix:**
1. Ensure `TranslationUpdateService.cs` has `MergeTranslations()` method
2. Update service should call `MergeTranslations()`, not `File.Move()`
3. Check app logs for "Merged X Aya translations" message

### Issue: Translation not updated

**Cause:** SuraId/AyaId mismatch between CSV and template database.

**Fix:**
1. Verify CSV has correct SureNo and AyahNo values
2. Check template database: `SELECT * FROM Aya WHERE SuraId = X AND AyaId = Y`
3. Ensure CSV uses same numbering as database

## Migration from Old Format

If you have databases created with the OLD converter:

1. **Identify the issue:**
   ```sql
   SELECT COUNT(*) FROM Aya WHERE Id = 0;
   ```
   If > 0, database was created incorrectly.

2. **Use correct template:**
   Get a clean quran.db from your app bundle that has:
   - All 114 Suras with metadata
   - All 6236 Ayas with sequential Ids (1-6236)

3. **Re-run conversion:**
   ```bash
   dotnet run YourCSV.csv CleanTemplate.db NewOutput.db 2025.11.11
   ```

4. **Verify before deployment:**
   Check that all Aya Ids are unique and sequential.

## Testing Before Deployment

1. **Convert CSV:**
   ```bash
   dotnet run test.csv template.db test_output.db 2025.11.11
   ```

2. **Inspect output:**
   ```bash
   sqlite3 test_output.db
   .schema Aya
   SELECT COUNT(DISTINCT Id) FROM Aya;  -- Should be 6236
   SELECT MIN(Id), MAX(Id) FROM Aya;    -- Should be 1, 6236
   SELECT * FROM Aya LIMIT 5;           -- Check translations
   ```

3. **Test in app:**
   - Copy test_output.db to app's Resources/Raw/
   - Run app and verify verses display correctly
   - Create test note and bookmark
   - Run update process
   - Verify note and bookmark still exist

## Summary

✅ **DO:**
- Use template database (your bundled quran.db)
- Provide 3 parameters: CSV, Template, Output
- Verify Aya Ids are sequential after conversion
- Test merge process before production deployment

❌ **DON'T:**
- Delete and recreate database from scratch
- Use AutoIncrement without preserving existing Ids
- Replace database file during updates (use merge instead)
- Skip validation checks

---

**Updated:** 2025-11-11  
**Version:** 2.0 (User Data Preservation Update)
