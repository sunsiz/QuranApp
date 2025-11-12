using CsvHelper;
using CsvHelper.Configuration;
using SQLite;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CsvToSqlite;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("QuranBlazor CSV to SQLite Converter");
        Console.WriteLine("===================================\n");

        if (args.Length < 3)
        {
            Console.WriteLine("Usage: CsvToSqlite <input.csv> <template.db> <output.db> [version]");
            Console.WriteLine("Example: CsvToSqlite Uzbek_Meal20251111.csv quran_template.db quran.db 2025.11.11");
            Console.WriteLine();
            Console.WriteLine("Parameters:");
            Console.WriteLine("  input.csv     - CSV file with updated translations");
            Console.WriteLine("  template.db   - Existing database to use as template (preserves structure)");
            Console.WriteLine("  output.db     - Output database file");
            Console.WriteLine("  version       - Version number (YYYY.MM.DD format, optional)");
            Console.WriteLine();
            Console.WriteLine("Note: The template database should be the bundled quran.db from your app's Resources/Raw folder.");
            Console.WriteLine("      This ensures all Sura metadata, table structure, and schema are preserved.");
            return 1;
        }

        string csvPath = args[0];
        string templateDbPath = args[1];
        string outputDbPath = args[2];
        string version = args.Length > 3 ? args[3] : DateTime.Now.ToString("yyyy.MM.dd");

        if (!File.Exists(csvPath))
        {
            Console.WriteLine($"Error: CSV file not found: {csvPath}");
            return 1;
        }

        if (!File.Exists(templateDbPath))
        {
            Console.WriteLine($"Error: Template database not found: {templateDbPath}");
            Console.WriteLine($"\nThe template database should be your existing quran.db file that contains:");
            Console.WriteLine($"  - Sura table with all metadata (Id, Name, ArabicName, AyaCount, RevealedIn)");
            Console.WriteLine($"  - Aya table structure");
            Console.WriteLine($"  - Any other tables your app needs");
            Console.WriteLine($"\nYou can find this in: QuranBlazor/Resources/Raw/quran.db");
            return 1;
        }

        try
        {
            // Copy template database to output
            Console.WriteLine($"Copying template database...");
            Console.WriteLine($"  From: {templateDbPath}");
            Console.WriteLine($"  To:   {outputDbPath}");
            File.Copy(templateDbPath, outputDbPath, overwrite: true);

            // Update translations in the copied database
            var stats = await UpdateTranslationsFromCsv(csvPath, outputDbPath);
            
            Console.WriteLine($"\n✓ Translation update completed successfully!");
            Console.WriteLine($"  Updated Ayas: {stats.AyaCount}");
            Console.WriteLine($"  Suras in database: {stats.SuraCount}");
            Console.WriteLine($"  Database: {outputDbPath}");
            Console.WriteLine($"  Size: {new FileInfo(outputDbPath).Length / 1024} KB");

            // Generate checksum
            string checksum = CalculateSHA256(outputDbPath);
            Console.WriteLine($"  SHA256: {checksum}");

            // Generate manifest file
            var manifest = new
            {
                version = version,
                date = DateTime.UtcNow.ToString("o"),
                fileName = "quran.db", // Always use standard name - app expects this
                size = new FileInfo(outputDbPath).Length,
                sha256 = checksum,
                suraCount = stats.SuraCount,
                ayaCount = stats.AyaCount,
                minAppVersion = "1.0.0"
            };

            // Use consistent naming: quran.json (not versioned)
            string manifestPath = Path.Combine(Path.GetDirectoryName(outputDbPath) ?? ".", "quran.json");
            await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));
            
            Console.WriteLine($"\n✓ Manifest generated: {manifestPath}");
            Console.WriteLine($"\nReady to upload to your server:");
            Console.WriteLine($"  1. Rename '{outputDbPath}' to 'quran.db'");
            Console.WriteLine($"  2. Upload 'quran.db' to your server");
            Console.WriteLine($"  3. Upload 'quran.json' to your server");
            Console.WriteLine($"\nBoth files must be in the same folder accessible via your update URL.");
            Console.WriteLine($"Example: https://yourdomain.com/quran/quran.db");
            Console.WriteLine($"         https://yourdomain.com/quran/quran.json");
            Console.WriteLine($"\n⚠ IMPORTANT: This database preserves all structure from the template.");
            Console.WriteLine($"            Only Aya translation fields (Text, Comment, DetailComment) were updated.");
            Console.WriteLine($"            User data tables (Notes, BookmarkCollections, etc.) are NOT included.");

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static char DetectDelimiter(string csvPath)
    {
        // Read first line to detect delimiter
        using (var reader = new StreamReader(csvPath, Encoding.UTF8))
        {
            var firstLine = reader.ReadLine();
            if (firstLine == null)
                return ',';
            
            // Count tabs vs commas
            int tabCount = firstLine.Count(c => c == '\t');
            int commaCount = firstLine.Count(c => c == ',');
            
            return tabCount > commaCount ? '\t' : ',';
        }
    }

    static async Task<ImportStats> UpdateTranslationsFromCsv(string csvPath, string dbPath)
    {
        Console.WriteLine($"\nReading CSV: {csvPath}");
        
        // Detect delimiter (tab or comma)
        var delimiter = DetectDelimiter(csvPath);
        Console.WriteLine($"Detected delimiter: {(delimiter == '\t' ? "Tab" : "Comma")}");
        
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null, // Ignore bad data
            MissingFieldFound = null,
            Delimiter = delimiter.ToString()
        };

        var updates = new List<AyaUpdate>();
        int lineNumber = 0;

        using (var reader = new StreamReader(csvPath, Encoding.UTF8))
        using (var csv = new CsvReader(reader, config))
        {
            await csv.ReadAsync();
            csv.ReadHeader();
            
            // Debug: Print header names
            Console.WriteLine($"CSV Headers: {string.Join(", ", csv.HeaderRecord)}");

            while (await csv.ReadAsync())
            {
                lineNumber++;
                
                try
                {
                    // Try both column name formats: "Sura"/"SureNo" and "Aya"/"AyahNo"
                    int suraId = 0;
                    int ayaId = 0;
                    
                    if (csv.TryGetField<int>("Sura", out var sura))
                        suraId = sura;
                    else if (csv.TryGetField<int>("SureNo", out var sureNo))
                        suraId = sureNo;
                    
                    if (csv.TryGetField<int>("Aya", out var aya))
                        ayaId = aya;
                    else if (csv.TryGetField<int>("AyahNo", out var ayahNo))
                        ayaId = ayahNo;
                    
                    string text = CleanString(csv.GetField<string>("Text"));
                    string comment = CleanString(csv.GetField<string>("Comment"));
                    string detailComment = CleanString(csv.GetField<string>("DetailComment"));

                    // Validate SuraId and AyaId
                    if (suraId <= 0 || suraId > 114)
                    {
                        Console.WriteLine($"  Skipping line {lineNumber}: Invalid SuraId {suraId}");
                        continue;
                    }

                    if (ayaId <= 0)
                    {
                        Console.WriteLine($"  Skipping line {lineNumber}: Invalid AyaId {ayaId}");
                        continue;
                    }

                    // Skip empty translations
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Console.WriteLine($"  Skipping line {lineNumber}: Empty translation text");
                        continue;
                    }

                    updates.Add(new AyaUpdate
                    {
                        SuraId = suraId,
                        AyaId = ayaId,
                        Text = text ?? "",
                        Comment = comment ?? "",
                        DetailComment = detailComment ?? ""
                    });

                    if (lineNumber % 500 == 0)
                    {
                        Console.WriteLine($"  Processed {lineNumber} lines...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Warning: Error on line {lineNumber}: {ex.Message}");
                }
            }
        }

        Console.WriteLine($"\nUpdating database: {dbPath}");
        Console.WriteLine($"  Translation updates to apply: {updates.Count}");

        // Open database and update Aya table
        var db = new SQLiteConnection(dbPath);
        
        int updatedCount = 0;
        int notFoundCount = 0;

        // Update ayas in batches
        db.BeginTransaction();
        try
        {
            foreach (var update in updates)
            {
                // Find existing Aya by SuraId and AyaId
                var existingAya = db.Table<Aya>()
                    .FirstOrDefault(a => a.SuraId == update.SuraId && a.AyaId == update.AyaId);

                if (existingAya != null)
                {
                    // Update only translation fields (NOT Arabic - it doesn't change)
                    // Preserve Id, IsFavorite, HasNote, Arabic
                    existingAya.Text = update.Text;
                    existingAya.Comment = update.Comment;
                    existingAya.DetailComment = update.DetailComment;
                    
                    db.Update(existingAya);
                    updatedCount++;

                    if (updatedCount % 500 == 0)
                    {
                        Console.WriteLine($"  Updated {updatedCount} ayas...");
                    }
                }
                else
                {
                    notFoundCount++;
                    Console.WriteLine($"  Warning: Aya not found in database: Sura {update.SuraId}, Aya {update.AyaId}");
                }
            }

            db.Commit();
        }
        catch (Exception ex)
        {
            db.Rollback();
            throw new Exception($"Failed to update database: {ex.Message}", ex);
        }

        // Get statistics
        int suraCount = db.Table<Sura>().Count();
        int ayaCount = db.Table<Aya>().Count();

        Console.WriteLine($"\n  Successfully updated: {updatedCount} ayas");
        if (notFoundCount > 0)
        {
            Console.WriteLine($"  Not found in database: {notFoundCount} ayas");
        }

        db.Close();

        return new ImportStats
        {
            SuraCount = suraCount,
            AyaCount = ayaCount
        };
    }

    static string? CleanString(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        // Trim and normalize whitespace
        input = input.Trim();
        
        // Remove multiple spaces
        while (input.Contains("  "))
        {
            input = input.Replace("  ", " ");
        }

        // Remove carriage returns and normalize line breaks
        input = input.Replace("\r\n", "\n").Replace("\r", "\n");
        
        // Remove leading/trailing newlines
        input = input.Trim('\n', '\r');

        return string.IsNullOrWhiteSpace(input) ? null : input;
    }

    static string CalculateSHA256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}

class ImportStats
{
    public int SuraCount { get; set; }
    public int AyaCount { get; set; }
}

class AyaUpdate
{
    public int SuraId { get; set; }
    public int AyaId { get; set; }
    // Note: Arabic field is NOT included - it doesn't change, only translations do
    public string Text { get; set; } = "";
    public string Comment { get; set; } = "";
    public string DetailComment { get; set; } = "";
}

// Database Models - Must match QuranBlazor.Data models exactly
public class Sura
{
    [PrimaryKey]
    public int Id { get; set; }
    
    [Collation("NOCASE")]
    public string Name { get; set; } = "";
    
    public string ArabicName { get; set; } = "";
    
    public int AyaCount { get; set; }
    
    public bool RevealedIn { get; set; }
}

public class Aya
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    public int SuraId { get; set; }
    
    public int AyaId { get; set; }
    
    [Collation("NOCASE")]
    public string Text { get; set; } = "";
    
    public string Arabic { get; set; } = "";
    
    [Collation("NOCASE")]
    public string? Comment { get; set; }
    
    [Collation("NOCASE")]
    public string? DetailComment { get; set; }
    
    public bool IsFavorite { get; set; }
    
    public bool HasNote { get; set; }
}

public class Note
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    public int SuraId { get; set; }
    
    public int AyaId { get; set; }
    
    [Collation("NOCASE")]
    public string Title { get; set; } = "";
    
    [Collation("NOCASE")]
    public string Content { get; set; } = "";
}
