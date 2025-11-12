using Microsoft.Maui.ApplicationModel;
using QuranBlazor.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace QuranBlazor.Services
{
    /// <summary>
    /// Service for exporting and backing up user data (notes, favorites)
    /// </summary>
    public class ExportService
    {
        private readonly DbContext _dbContext;

        public ExportService(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public class ExportData
        {
            public List<NoteExport> Notes { get; set; }
            public List<FavoriteExport> Favorites { get; set; }
            public string ExportDate { get; set; }
            public string AppVersion { get; set; }
        }

        public class NoteExport
        {
            public int SuraId { get; set; }
            public int AyaId { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
        }

        public class FavoriteExport
        {
            public int SuraId { get; set; }
            public int AyaId { get; set; }
            public string SuraName { get; set; }
            public string Text { get; set; }
        }

        /// <summary>
        /// Exports all user data to JSON string
        /// </summary>
        public async Task<string> ExportToJsonAsync()
        {
            try
            {
                var notes = await _dbContext.GetNotesAsync();
                var favorites = await _dbContext.GetFavoritesAsync();

                var exportData = new ExportData
                {
                    Notes = notes.Select(n => new NoteExport
                    {
                        SuraId = n.SuraId,
                        AyaId = n.AyaId,
                        Title = n.Title,
                        Content = n.Content
                    }).ToList(),
                    Favorites = favorites.Select(f => new FavoriteExport
                    {
                        SuraId = f.SuraId,
                        AyaId = f.AyaId,
                        SuraName = f.SuraName,
                        Text = f.Text
                    }).ToList(),
                    ExportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    AppVersion = "1.3"
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                return JsonSerializer.Serialize(exportData, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting data: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Exports data to a file
        /// </summary>
        public async Task<string> ExportToFileAsync()
        {
            try
            {
                var json = await ExportToJsonAsync();
                var fileName = $"QuranApp_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);

                return filePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting to file: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Shares the export file using the platform share dialog
        /// </summary>
        public async Task ShareBackupAsync()
        {
            try
            {
                var filePath = await ExportToFileAsync();
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = TransliterationService.GetDisplayText("Қурон иловаси маълумотларини заҳиралаш"),
                        File = new ShareFile(filePath)
                    });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sharing backup: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets statistics about user data
        /// </summary>
        public async Task<(int NotesCount, int FavoritesCount)> GetDataStatisticsAsync()
        {
            try
            {
                var notes = await _dbContext.GetNotesAsync();
                var favorites = await _dbContext.GetFavoritesAsync();
                return (notes.Count, favorites.Count);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting statistics: {ex.Message}");
                return (0, 0);
            }
        }

        /// <summary>
        /// Imports user data from JSON string
        /// </summary>
        public async Task<(int NotesImported, int FavoritesImported, string Error)> ImportFromJsonAsync(string jsonContent)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var importData = JsonSerializer.Deserialize<ExportData>(jsonContent, options);
                
                if (importData == null)
                {
                    return (0, 0, "JSON формати нотўғри");
                }

                int notesImported = 0;
                int favoritesImported = 0;

                // Cache sura metadata and aya lists to avoid repetitive database round-trips
                var suraLookup = (await _dbContext.GetSuraListAsync()).ToDictionary(s => s.Id, s => s.Name);
                var ayaCache = new Dictionary<int, List<Aya>>();

                async Task<Aya> GetAyaAsync(int suraId, int ayaNumber)
                {
                    if (!ayaCache.TryGetValue(suraId, out var ayasForSura))
                    {
                        ayasForSura = await _dbContext.GetAyaListAsync(suraId);
                        ayaCache[suraId] = ayasForSura;
                    }

                    return ayasForSura.FirstOrDefault(a => a.AyaId == ayaNumber);
                }

                // Import Notes
                if (importData.Notes != null)
                {
                    foreach (var noteData in importData.Notes)
                    {
                        try
                        {
                            // Check if note already exists
                            var existingNote = await _dbContext.GetNoteAsync(noteData.AyaId, noteData.SuraId);
                            
                            if (existingNote != null)
                            {
                                // Update existing note
                                existingNote.Title = noteData.Title;
                                existingNote.Content = noteData.Content;
                                await _dbContext.UpdateNoteAsync(existingNote);
                            }
                            else
                            {
                                // Create new note - use cached sura name where possible
                                suraLookup.TryGetValue(noteData.SuraId, out var suraName);
                                suraName ??= "Номаълум";

                                await _dbContext.AddNoteAsync(noteData.AyaId, noteData.SuraId, suraName, noteData.Content);
                            }
                            notesImported++;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error importing note {noteData.SuraId}:{noteData.AyaId}: {ex.Message}");
                        }
                    }
                }

                // Import Favorites
                if (importData.Favorites != null)
                {
                    foreach (var favoriteData in importData.Favorites)
                    {
                        try
                        {
                            // Get all ayas for the sura and find the specific aya
                            var aya = await GetAyaAsync(favoriteData.SuraId, favoriteData.AyaId);
                            
                            if (aya != null && !aya.IsFavorite)
                            {
                                aya.IsFavorite = true;
                                await _dbContext.UpdateAyaAsync(aya);
                                favoritesImported++;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error importing favorite {favoriteData.SuraId}:{favoriteData.AyaId}: {ex.Message}");
                        }
                    }
                }

                return (notesImported, favoritesImported, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing data: {ex.Message}");
                return (0, 0, "Маълумотларни импорт қилишда хатолик юз берди");
            }
        }

        /// <summary>
        /// Picks a file and imports data from it
        /// </summary>
        public async Task<(int NotesImported, int FavoritesImported, string Error)> ImportFromFileAsync()
        {
            try
            {
                var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.iOS, new[] { "public.json" } },
                    { DevicePlatform.Android, new[] { "application/json" } },
                    { DevicePlatform.WinUI, new[] { ".json" } },
                    { DevicePlatform.MacCatalyst, new[] { "json" } },
                });

                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    FileTypes = customFileType,
                    PickerTitle = TransliterationService.GetDisplayText("Заҳира файлини танланг")
                });

                if (result == null)
                {
                    return (0, 0, "No file selected");
                }

                using var stream = await result.OpenReadAsync();
                try
                {
                    if (stream.CanSeek && stream.Length > 512_000)
                    {
                        return (0, 0, "Танланган файл ҳажми жуда катта");
                    }
                }
                catch (NotSupportedException)
                {
                    // Ignore if stream does not support length
                }

                using var reader = new StreamReader(stream);
                var jsonContent = await reader.ReadToEndAsync();

                if (jsonContent.Length > 512_000)
                {
                    return (0, 0, "Танланган файл ҳажми жуда катта");
                }

                return await ImportFromJsonAsync(jsonContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing from file: {ex.Message}");
                return (0, 0, "Маълумотларни импорт қилишда хатолик юз берди");
            }
        }
    }
}
