using System.Diagnostics;

namespace QuranBlazor.Services
{
    /// <summary>
    /// Service for checking and downloading translation database updates
    /// </summary>
    public class TranslationUpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly string _updateBaseUrl;
        private const string ManifestFileName = "quran.json";
        private const string DatabaseFileName = "quran.db";

        public TranslationUpdateService(string updateBaseUrl)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
            _updateBaseUrl = updateBaseUrl.TrimEnd('/');
        }

        public class UpdateInfo
        {
            public string Version { get; set; } = "";
            public DateTime Date { get; set; }
            public string FileName { get; set; } = "";
            public long Size { get; set; }
            public string Sha256 { get; set; } = "";
            public int SuraCount { get; set; }
            public int AyaCount { get; set; }
            public string MinAppVersion { get; set; } = "";
        }

        public class UpdateProgress
        {
            public long BytesDownloaded { get; set; }
            public long TotalBytes { get; set; }
            public int ProgressPercentage => TotalBytes > 0 ? (int)((BytesDownloaded * 100) / TotalBytes) : 0;
            public string Status { get; set; } = "";
        }

        /// <summary>
        /// Check if a new translation update is available
        /// </summary>
        public async Task<(bool Available, UpdateInfo Info, string Error)> CheckForUpdateAsync()
        {
            try
            {
                var currentVersion = GetCurrentDatabaseVersion();
                var manifestUrl = $"{_updateBaseUrl}/{ManifestFileName}";

                Debug.WriteLine($"Checking for updates at: {manifestUrl}");

                var response = await _httpClient.GetAsync(manifestUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    return (false, null, $"Failed to check for updates: HTTP {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var updateInfo = System.Text.Json.JsonSerializer.Deserialize<UpdateInfo>(json, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (updateInfo == null)
                {
                    return (false, null, "Invalid update manifest");
                }

                // Compare versions
                if (CompareVersions(updateInfo.Version, currentVersion) > 0)
                {
                    Debug.WriteLine($"Update available: {updateInfo.Version} (current: {currentVersion})");
                    return (true, updateInfo, "");
                }

                Debug.WriteLine($"Already up to date: {currentVersion}");
                return (false, updateInfo, "");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking for updates: {ex.Message}");
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Download and install translation update by merging Aya translations while preserving user data
        /// </summary>
        public async Task<(bool Success, string Error)> DownloadAndInstallUpdateAsync(
            UpdateInfo updateInfo,
            IProgress<UpdateProgress> progress = null)
        {
            var tempPath = "";
            var backupPath = "";

            try
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
                tempPath = Path.Combine(FileSystem.CacheDirectory, $"{DatabaseFileName}.tmp");
                backupPath = Path.Combine(FileSystem.AppDataDirectory, $"{DatabaseFileName}.backup");

                // Download to temp file
                progress?.Report(new UpdateProgress { Status = "Юкланмоқда..." });
                
                var downloadUrl = $"{_updateBaseUrl}/{updateInfo.FileName}";
                Debug.WriteLine($"Downloading from: {downloadUrl}");

                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        return (false, $"Download failed: HTTP {response.StatusCode}");
                    }

                    var totalBytes = response.Content.Headers.ContentLength ?? updateInfo.Size;
                    
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;

                            progress?.Report(new UpdateProgress
                            {
                                BytesDownloaded = totalRead,
                                TotalBytes = totalBytes,
                                Status = $"Юкланди: {FormatBytes(totalRead)} / {FormatBytes(totalBytes)}"
                            });
                        }
                    }
                }

                // Verify checksum
                progress?.Report(new UpdateProgress { Status = "Текширилмоқда..." });
                
                var actualChecksum = CalculateSHA256(tempPath);
                if (!actualChecksum.Equals(updateInfo.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(tempPath);
                    return (false, "Checksum verification failed - файл бузилган");
                }

                Debug.WriteLine("Checksum verified successfully");

                // Backup current database
                progress?.Report(new UpdateProgress { Status = "Заҳиралаш..." });
                
                if (File.Exists(dbPath))
                {
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                    File.Copy(dbPath, backupPath); // Use Copy instead of Move so we can still access it
                    Debug.WriteLine("Current database backed up");
                }

                // Merge translations from downloaded database into existing database
                progress?.Report(new UpdateProgress { Status = "Таржималар янгиланмоқда..." });
                
                var mergeResult = MergeTranslations(tempPath, dbPath);
                if (!mergeResult.Success)
                {
                    // Restore backup on merge failure
                    if (File.Exists(backupPath))
                    {
                        File.Copy(backupPath, dbPath, overwrite: true);
                    }
                    return (false, $"Merge failed: {mergeResult.Error}");
                }

                Debug.WriteLine($"Merged {mergeResult.UpdatedCount} Aya translations");

                // Update version preference
                Preferences.Set(PreferenceKeys.DatabaseVersion, updateInfo.Version);
                Preferences.Set(PreferenceKeys.DatabaseUpdateDate, DateTime.Now.ToString("o"));

                Debug.WriteLine($"Update installed successfully: {updateInfo.Version}");

                // Clean up backup and temp after successful install
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                return (true, "");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error installing update: {ex.Message}");

                // Cleanup temp file
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }

                // Restore backup if exists
                if (!string.IsNullOrEmpty(backupPath) && File.Exists(backupPath))
                {
                    try
                    {
                        var dbPath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
                        File.Copy(backupPath, dbPath, overwrite: true);
                        Debug.WriteLine("Database restored from backup");
                    }
                    catch (Exception restoreEx)
                    {
                        Debug.WriteLine($"Failed to restore backup: {restoreEx.Message}");
                    }
                }

                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Merge Aya translations from source database to target database while preserving user data
        /// </summary>
        private (bool Success, string Error, int UpdatedCount) MergeTranslations(string sourceDbPath, string targetDbPath)
        {
            SQLite.SQLiteConnection sourceDb = null;
            SQLite.SQLiteConnection targetDb = null;
            
            try
            {
                sourceDb = new SQLite.SQLiteConnection(sourceDbPath);
                targetDb = new SQLite.SQLiteConnection(targetDbPath);

                // Get all Ayas from source (new translations)
                var sourceAyas = sourceDb.Table<Data.Aya>().ToList();
                
                Debug.WriteLine($"Source database contains {sourceAyas.Count} Ayas");

                int updatedCount = 0;

                targetDb.BeginTransaction();
                try
                {
                    foreach (var sourceAya in sourceAyas)
                    {
                        // Find matching Aya in target by SuraId and AyaId
                        var targetAya = targetDb.Table<Data.Aya>()
                            .FirstOrDefault(a => a.SuraId == sourceAya.SuraId && a.AyaId == sourceAya.AyaId);

                        if (targetAya != null)
                        {
                            // Update only translation fields (NOT Arabic - it doesn't change)
                            // Preserve user data (IsFavorite, HasNote, Id) and Arabic text
                            targetAya.Text = sourceAya.Text;
                            targetAya.Comment = sourceAya.Comment;
                            targetAya.DetailComment = sourceAya.DetailComment;

                            targetDb.Update(targetAya);
                            updatedCount++;
                        }
                        else
                        {
                            Debug.WriteLine($"Warning: Aya not found in target: Sura {sourceAya.SuraId}, Aya {sourceAya.AyaId}");
                        }
                    }

                    targetDb.Commit();
                    Debug.WriteLine($"Successfully updated {updatedCount} Ayas");
                }
                catch (Exception ex)
                {
                    targetDb.Rollback();
                    throw new Exception($"Transaction failed: {ex.Message}", ex);
                }

                return (true, "", updatedCount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error merging translations: {ex.Message}");
                return (false, ex.Message, 0);
            }
            finally
            {
                sourceDb?.Close();
                targetDb?.Close();
            }
        }

        /// <summary>
        /// Get current installed database version
        /// </summary>
        public string GetCurrentDatabaseVersion()
        {
            return Preferences.Get(PreferenceKeys.DatabaseVersion, "2.0");
        }

        /// <summary>
        /// Get last update check date
        /// </summary>
        public DateTime? GetLastUpdateCheck()
        {
            var lastCheck = Preferences.Get(PreferenceKeys.LastUpdateCheck, "");
            if (DateTime.TryParse(lastCheck, out var date))
            {
                return date;
            }
            return null;
        }

        /// <summary>
        /// Record that we checked for updates
        /// </summary>
        public void RecordUpdateCheck()
        {
            Preferences.Set(PreferenceKeys.LastUpdateCheck, DateTime.Now.ToString("o"));
        }

        /// <summary>
        /// Compare version strings (semantic versioning: YYYY.MM.DD)
        /// </summary>
        private int CompareVersions(string version1, string version2)
        {
            var v1Parts = version1.Split('.').Select(int.Parse).ToArray();
            var v2Parts = version2.Split('.').Select(int.Parse).ToArray();

            for (int i = 0; i < Math.Min(v1Parts.Length, v2Parts.Length); i++)
            {
                if (v1Parts[i] != v2Parts[i])
                {
                    return v1Parts[i].CompareTo(v2Parts[i]);
                }
            }

            return v1Parts.Length.CompareTo(v2Parts.Length);
        }

        private string CalculateSHA256(string filePath)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
