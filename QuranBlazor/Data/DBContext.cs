using SQLite;
using System.Diagnostics;
using Microsoft.Data.Sqlite;

namespace QuranBlazor.Data
{
    public class DbContext
    {
        private readonly string _dbPath;
        private SQLiteAsyncConnection _conn;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private bool _initialized = false;

        public DbContext(string dbPath)
        {
            _dbPath = dbPath;
            // Don't block here - initialize lazily on first use
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            await _initLock.WaitAsync();
            try
            {
                if (_initialized) return;
                await InitializeAsync();
                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task InitializeAsync()
        {
            // Create database and Tables
            if (!File.Exists(_dbPath)) await CopyFileToAppDataDirectory();
            _conn = new SQLiteAsyncConnection(_dbPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            try
            {
                // Create bookmark collection tables if they don't exist
                await _conn.CreateTableAsync<BookmarkCollection>();
                await _conn.CreateTableAsync<AyaBookmarkCollection>();

                // Create reading progress tables if they don't exist
                await _conn.CreateTableAsync<ReadingProgress>();
                await _conn.CreateTableAsync<AyaReadStatus>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating tables: {ex.Message}");
                // Tables might already exist, continue anyway
            }

#if DEBUG
            _conn.Tracer = new Action<string>(q => Debug.WriteLine(q));
            _conn.Trace = true;
#endif
        }

        private async Task CopyFileToAppDataDirectory()
        {
            var filename = @"quran.db";
            var targetPath = Path.Combine(FileSystem.Current.AppDataDirectory, filename);
            if (File.Exists(targetPath))
            {
                Debug.WriteLine($"Target file already exists: {targetPath}");
                return;
            }

            try
            {
                Stream sourceStream;
#if WINDOWS
        sourceStream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, filename));
#else
                sourceStream = await Microsoft.Maui.Storage.FileSystem.Current.OpenAppPackageFileAsync(filename);
#endif
                using (sourceStream)
                {
                    using var targetStream = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write);
                    sourceStream.CopyTo(targetStream);
                    Debug.WriteLine("File copied successfully");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        public async Task<List<Sura>> GetSuraListAsync()
        {
            await EnsureInitializedAsync();
            return await _conn.Table<Sura>().ToListAsync();
        }

        public async Task<List<Aya>> GetAyaListAsync(int suraId)
        {
            await EnsureInitializedAsync();
            return await _conn.Table<Aya>().Where(a => a.SuraId == suraId).ToListAsync();
        }

        public async Task<int> UpdateAyaAsync(Aya aya)
        {
            await EnsureInitializedAsync();
            var result = await _conn.UpdateAsync(aya);
            Debug.WriteLine(result);
            return result;
        }

        public async Task<Note> GetNoteAsync(int ayaId, int suraId)
        {
            await EnsureInitializedAsync();
            return await _conn.Table<Note>().FirstOrDefaultAsync(n => n.AyaId == ayaId && n.SuraId == suraId);
        }

        public async Task<int> AddNoteAsync(int ayaId, int suraId, string suraName, string note)
        {
            await EnsureInitializedAsync();
            var newNote = new Note()
            {
                AyaId = ayaId,
                Content = note,
                SuraId = suraId,
                Title = $"{suraId}. Сура {suraName}, {ayaId}. Оят"
            };
            var result = await _conn.InsertAsync(newNote);
            var aya = await _conn.Table<Aya>().FirstOrDefaultAsync(a => a.SuraId == suraId && a.AyaId == ayaId);
            aya.HasNote = true;
            await _conn.UpdateAsync(aya);
            Debug.WriteLine("Add note result is:" + result);

            return result;
        }

        public async Task<int> DeleteNoteAsync(int ayaId, int suraId)
        {
            await EnsureInitializedAsync();
            var noteResult = await _conn.Table<Note>().DeleteAsync(n => n.SuraId == suraId && n.AyaId == ayaId);
            Debug.WriteLine("Delete note result is:" + noteResult);

            var aya = await _conn.Table<Aya>().FirstOrDefaultAsync(a => a.SuraId == suraId && a.AyaId == ayaId);
            aya.HasNote = false;
            var result = await _conn.UpdateAsync(aya);
            Debug.WriteLine("Delete has note from aya result is:" + result);
            return result;
        }

        public async Task<int> UpdateNote(int ayaId, int suraId)
        {
            await EnsureInitializedAsync();
            int result = -1;
            var note = await _conn.Table<Note>().FirstOrDefaultAsync(n => n.SuraId == suraId && n.AyaId == ayaId);
            if (note != null)
            {
                result = await _conn.UpdateAsync(note);
                Debug.WriteLine(result);
            }
            return result;
        }

        public async Task<int> UpdateNoteAsync(Note note)
        {
            await EnsureInitializedAsync();
            if (note == null) return -1;
            var result = await _conn.UpdateAsync(note);
            Debug.WriteLine($"Update note result: {result}");
            return result;
        }

        public async Task<List<FavoritesViewModel>> GetFavoritesAsync()
        {
            await EnsureInitializedAsync();
            var favoriteAyas = await _conn.Table<Aya>().Where(a => a.IsFavorite).ToListAsync();
            var suras = await _conn.Table<Sura>().ToListAsync();

            var favorites = favoriteAyas.Select(a => new FavoritesViewModel
            {
                SuraId = a.SuraId,
                AyaId = a.AyaId,
                Text = a.Text,
                SuraName = suras.FirstOrDefault(s => s.Id == a.SuraId)?.Name
            }).ToList();

            return favorites;
        }

        public async Task<int> DeleteFavoriteAsync(int ayaId, int suraId)
        {
            await EnsureInitializedAsync();
            var aya = await _conn.Table<Aya>().FirstOrDefaultAsync(a => a.SuraId == suraId && a.AyaId == ayaId);
            aya.IsFavorite = false;
            var result = await _conn.UpdateAsync(aya);
            Debug.WriteLine("Delete favorite aya result is:" + result);
            return result;
        }

        public async Task<string> GetSuraNameAsync(int suraId)
        {
            await EnsureInitializedAsync();
            var sura = await _conn.Table<Sura>().FirstOrDefaultAsync(s => s.Id == suraId);
            return sura?.Name ?? string.Empty;
        }

        public async Task<List<Note>> GetNotesAsync()
        {
            await EnsureInitializedAsync();
            return await _conn.Table<Note>().ToListAsync();
        }

        /// <summary>
        /// This method is used to search for Ayas (verses of the Quran) that contain a specific keyword in their text or comment.
        /// Because SQLite does not support the Unicode character comparison well (can't perform case insensitive comparison on Cyrillic characters), we have to create a custom function to compare the text and the keyword.
        /// </summary>
        /// <param name="keyWord">The keyword is passed as a parameter to the method.</param>
        /// <returns>The list of Ayas that matches th condition</returns>
        public async Task<IEnumerable<Aya>> GetSearchResult(string keyWord)
        {
            await EnsureInitializedAsync();
            if (string.IsNullOrEmpty(keyWord)) return null;
            var ayas = new List<Aya>();
            using var connection = new SqliteConnection("Data Source=" + _dbPath);
            // A custom SQLite function "ContainsKeyword" is created.
            // This function checks if a field value contains the keyword, ignoring case.
            connection.CreateFunction(
                "ContainsKeyword",
                (string fieldValue, string keyword) => fieldValue != null && keyword != null && fieldValue.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            );
            using var command = connection.CreateCommand();
            command.CommandText = @"SELECT * FROM Aya WHERE ContainsKeyword(Text, $kw) or ContainsKeyword(Comment, $kw);";
            command.Parameters.AddWithValue("$kw", keyWord);

            connection.Open();
            using var oReader = command.ExecuteReader();
            while (oReader.Read())
            {
                var aya = new Aya()
                {
                    Id = oReader.GetInt32(0),
                    SuraId = oReader.GetInt32(1),
                    AyaId = oReader.GetInt32(2),
                    Text = oReader.GetString(3),
                    Arabic = oReader.GetString(4),
                    Comment = oReader.IsDBNull(5) ? null : oReader.GetString(5),
                    DetailComment = oReader.IsDBNull(6) ? null : oReader.GetString(6),
                    IsFavorite = !oReader.IsDBNull(7) && oReader.GetBoolean(7),
                    HasNote = !oReader.IsDBNull(8) && oReader.GetBoolean(8)
                };
                ayas.Add(aya);
            }
            return ayas;
        }

        #region Bookmark Collections

        /// <summary>
        /// Get all bookmark collections
        /// </summary>
        public async Task<List<BookmarkCollection>> GetBookmarkCollectionsAsync()
        {
            await EnsureInitializedAsync();
            return await _conn.Table<BookmarkCollection>().OrderBy(c => c.DisplayOrder).ToListAsync();
        }

        /// <summary>
        /// Get a specific bookmark collection by ID
        /// </summary>
        public async Task<BookmarkCollection> GetBookmarkCollectionAsync(int collectionId)
        {
            await EnsureInitializedAsync();
            return await _conn.Table<BookmarkCollection>().FirstOrDefaultAsync(c => c.Id == collectionId);
        }

        /// <summary>
        /// Create a new bookmark collection
        /// </summary>
        public async Task<int> AddBookmarkCollectionAsync(BookmarkCollection collection)
        {
            await EnsureInitializedAsync();
            if (collection == null) return -1;
            
            collection.CreatedDate = DateTime.Now;
            
            // Set display order to max + 1 if not specified
            if (collection.DisplayOrder == 0)
            {
                var maxOrder = await _conn.Table<BookmarkCollection>().CountAsync();
                collection.DisplayOrder = maxOrder + 1;
            }
            
            var result = await _conn.InsertAsync(collection);
            Debug.WriteLine($"Add bookmark collection result: {result}");
            return result;
        }

        /// <summary>
        /// Update a bookmark collection
        /// </summary>
        public async Task<int> UpdateBookmarkCollectionAsync(BookmarkCollection collection)
        {
            await EnsureInitializedAsync();
            if (collection == null) return -1;
            var result = await _conn.UpdateAsync(collection);
            Debug.WriteLine($"Update bookmark collection result: {result}");
            return result;
        }

        /// <summary>
        /// Delete a bookmark collection and all its associations
        /// </summary>
        public async Task<int> DeleteBookmarkCollectionAsync(int collectionId)
        {
            await EnsureInitializedAsync();
            // Delete all associations first
            await _conn.Table<AyaBookmarkCollection>().DeleteAsync(abc => abc.CollectionId == collectionId);
            
            var result = await _conn.Table<BookmarkCollection>().DeleteAsync(c => c.Id == collectionId);
            Debug.WriteLine($"Delete bookmark collection result: {result}");
            return result;
        }

        /// <summary>
        /// Add an Aya to a bookmark collection
        /// </summary>
        public async Task<int> AddAyaToCollectionAsync(int ayaId, int collectionId, string notes = null)
        {
            await EnsureInitializedAsync();
            // Check if already exists
            var existing = await _conn.Table<AyaBookmarkCollection>()
                .FirstOrDefaultAsync(abc => abc.AyaId == ayaId && abc.CollectionId == collectionId);
            
            if (existing != null)
            {
                Debug.WriteLine("Aya already in collection");
                return 0;
            }

            var newAssociation = new AyaBookmarkCollection
            {
                AyaId = ayaId,
                CollectionId = collectionId,
                AddedDate = DateTime.Now,
                Notes = notes
            };

            var result = await _conn.InsertAsync(newAssociation);
            Debug.WriteLine($"Add Aya to collection result: {result}");
            return result;
        }

        /// <summary>
        /// Remove an Aya from a bookmark collection
        /// </summary>
        public async Task<int> RemoveAyaFromCollectionAsync(int ayaId, int collectionId)
        {
            await EnsureInitializedAsync();
            var result = await _conn.Table<AyaBookmarkCollection>()
                .DeleteAsync(abc => abc.AyaId == ayaId && abc.CollectionId == collectionId);
            Debug.WriteLine($"Remove Aya from collection result: {result}");
            return result;
        }

        /// <summary>
        /// Get all Ayas in a specific bookmark collection
        /// </summary>
        public async Task<List<FavoritesViewModel>> GetAyasInCollectionAsync(int collectionId)
        {
            await EnsureInitializedAsync();
            var associations = await _conn.Table<AyaBookmarkCollection>()
                .Where(abc => abc.CollectionId == collectionId)
                .ToListAsync();

            var ayaIds = associations.Select(a => a.AyaId).ToList();
            var ayas = await _conn.Table<Aya>().Where(a => ayaIds.Contains(a.Id)).ToListAsync();
            var suras = await _conn.Table<Sura>().ToListAsync();

            var result = ayas.Select(a =>
            {
                var sura = suras.FirstOrDefault(s => s.Id == a.SuraId);
                return new FavoritesViewModel
                {
                    SuraId = a.SuraId,
                    AyaId = a.AyaId,
                    Text = a.Text,
                    SuraName = sura?.Name
                };
            }).ToList();

            return result;
        }

        /// <summary>
        /// Get all collections that contain a specific Aya
        /// </summary>
        public async Task<List<BookmarkCollection>> GetCollectionsForAyaAsync(int suraId, int ayaId)
        {
            await EnsureInitializedAsync();
            var aya = await _conn.Table<Aya>().FirstOrDefaultAsync(a => a.SuraId == suraId && a.AyaId == ayaId);
            if (aya == null) return new List<BookmarkCollection>();

            var associations = await _conn.Table<AyaBookmarkCollection>()
                .Where(abc => abc.AyaId == aya.Id)
                .ToListAsync();

            var collectionIds = associations.Select(a => a.CollectionId).ToList();
            var collections = await _conn.Table<BookmarkCollection>()
                .Where(c => collectionIds.Contains(c.Id))
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return collections;
        }

        /// <summary>
        /// Get count of Ayas in a collection
        /// </summary>
        public async Task<int> GetCollectionAyaCountAsync(int collectionId)
        {
            await EnsureInitializedAsync();
            return await _conn.Table<AyaBookmarkCollection>()
                .Where(abc => abc.CollectionId == collectionId)
                .CountAsync();
        }

        #endregion

        #region Reading Progress

        /// <summary>
        /// Get or create reading progress for a Sura
        /// </summary>
        public async Task<ReadingProgress> GetOrCreateReadingProgressAsync(int suraId)
        {
            await EnsureInitializedAsync();
            var progress = await _conn.Table<ReadingProgress>()
                .FirstOrDefaultAsync(rp => rp.SuraId == suraId);

            if (progress == null)
            {
                var ayas = await GetAyaListAsync(suraId);
                progress = new ReadingProgress
                {
                    SuraId = suraId,
                    TotalAyas = ayas.Count,
                    AyasRead = 0,
                    FirstReadDate = DateTime.Now,
                    LastReadDate = DateTime.Now,
                    IsCompleted = false
                };
                await _conn.InsertAsync(progress);
            }

            return progress;
        }

        /// <summary>
        /// Mark an Aya as read and update progress
        /// </summary>
        public async Task<int> MarkAyaAsReadAsync(int suraId, int ayaNumber)
        {
            await EnsureInitializedAsync();
            var aya = await _conn.Table<Aya>()
                .FirstOrDefaultAsync(a => a.SuraId == suraId && a.AyaId == ayaNumber);

            if (aya == null) return 0;

            // Check if already marked as read
            var readStatus = await _conn.Table<AyaReadStatus>()
                .FirstOrDefaultAsync(ars => ars.AyaId == aya.Id);

            if (readStatus == null)
            {
                readStatus = new AyaReadStatus
                {
                    AyaId = aya.Id,
                    SuraId = suraId,
                    AyaNumber = ayaNumber,
                    IsRead = true,
                    ReadDate = DateTime.Now,
                    ReadCount = 1
                };
                await _conn.InsertAsync(readStatus);
            }
            else if (!readStatus.IsRead)
            {
                readStatus.IsRead = true;
                readStatus.ReadDate = DateTime.Now;
                readStatus.ReadCount++;
                await _conn.UpdateAsync(readStatus);
            }
            else
            {
                // Already read, just increment count
                readStatus.ReadCount++;
                readStatus.ReadDate = DateTime.Now;
                await _conn.UpdateAsync(readStatus);
            }

            // Update Sura progress
            var progress = await GetOrCreateReadingProgressAsync(suraId);
            var readAyasCount = await _conn.Table<AyaReadStatus>()
                .Where(ars => ars.SuraId == suraId && ars.IsRead)
                .CountAsync();

            progress.AyasRead = readAyasCount;
            progress.LastReadDate = DateTime.Now;

            if (readAyasCount >= progress.TotalAyas && !progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedDate = DateTime.Now;
            }

            return await _conn.UpdateAsync(progress);
        }

        /// <summary>
        /// Get reading progress for all Suras that have been started
        /// </summary>
        public async Task<List<ReadingProgress>> GetAllReadingProgressAsync()
        {
            await EnsureInitializedAsync();
            try
            {
                return await _conn.Table<ReadingProgress>()
                    .OrderByDescending(rp => rp.LastReadDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting reading progress: {ex.Message}");
                return new List<ReadingProgress>();
            }
        }

        /// <summary>
        /// Get reading progress for a specific Sura
        /// </summary>
        public async Task<ReadingProgress> GetReadingProgressAsync(int suraId)
        {
            await EnsureInitializedAsync();
            return await _conn.Table<ReadingProgress>()
                .FirstOrDefaultAsync(rp => rp.SuraId == suraId);
        }

        /// <summary>
        /// Get overall reading statistics
        /// </summary>
        public async Task<(int TotalSuras, int CompletedSuras, int TotalAyas, int ReadAyas)> GetReadingStatisticsAsync()
        {
            try
            {
                // Use fixed Quran numbers - 114 Suras, 6236 Ayas
                const int TOTAL_SURAS = 114;
                const int TOTAL_AYAS = 6236;
                
                var allProgress = await GetAllReadingProgressAsync();
                var completedSuras = allProgress.Count(p => p.IsCompleted);
                
                var readAyas = await _conn.Table<AyaReadStatus>()
                    .Where(ars => ars.IsRead)
                    .CountAsync();

                return (TOTAL_SURAS, completedSuras, TOTAL_AYAS, readAyas);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting reading statistics: {ex.Message}");
                // Return default values on error
                return (114, 0, 6236, 0);
            }
        }

        /// <summary>
        /// Get read status for all Ayas in a Sura
        /// </summary>
        public async Task<List<AyaReadStatus>> GetAyaReadStatusForSuraAsync(int suraId)
        {
            await EnsureInitializedAsync();
            return await _conn.Table<AyaReadStatus>()
                .Where(ars => ars.SuraId == suraId)
                .ToListAsync();
        }

        /// <summary>
        /// Reset reading progress for a Sura
        /// </summary>
        public async Task<int> ResetSuraProgressAsync(int suraId)
        {
            await EnsureInitializedAsync();
            // Delete all Aya read statuses for this Sura
            await _conn.Table<AyaReadStatus>().DeleteAsync(ars => ars.SuraId == suraId);
            
            // Delete the progress record
            return await _conn.Table<ReadingProgress>().DeleteAsync(rp => rp.SuraId == suraId);
        }

        #endregion

        #region Favorites Migration

        /// <summary>
        /// Check if favorites need to be migrated to collections
        /// </summary>
        public bool NeedsFavoritesMigration()
        {
            return !Preferences.Get("FavoritesMigrated", false);
        }

        /// <summary>
        /// Mark favorites migration as complete
        /// </summary>
        public void MarkFavoritesMigrationComplete()
        {
            Preferences.Set("FavoritesMigrated", true);
            Debug.WriteLine("Favorites migration marked as complete");
        }

        /// <summary>
        /// Create default favorites collection if it doesn't exist
        /// Returns collection ID
        /// </summary>
        public async Task<int> CreateDefaultFavoritesCollectionAsync()
        {
            await EnsureInitializedAsync();
            
            // Check if favorites collection already exists (by name)
            var existing = await _conn.Table<BookmarkCollection>()
                .FirstOrDefaultAsync(c => c.Name == "❤️ Belgilangan oyatlar");
            
            if (existing != null)
            {
                Debug.WriteLine($"Favorites collection already exists with ID: {existing.Id}");
                return existing.Id;
            }

            var favoritesCollection = new BookmarkCollection
            {
                Name = "❤️ Belgilangan oyatlar",
                Description = "Sevimli oyatlaringiz",
                CreatedDate = DateTime.Now,
                DisplayOrder = 0,
                ColorCode = "#DC143C" // Crimson red for favorites
            };

            var result = await _conn.InsertAsync(favoritesCollection);
            Debug.WriteLine($"Created default favorites collection with ID: {favoritesCollection.Id}");
            
            return favoritesCollection.Id;
        }

        /// <summary>
        /// Migrate all favorite ayas to the default favorites collection
        /// </summary>
        public async Task<int> MigrateFavoritesToCollectionAsync(int favoritesCollectionId)
        {
            await EnsureInitializedAsync();
            
            // Get all favorite ayas
            var favorites = await _conn.Table<Aya>()
                .Where(a => a.IsFavorite)
                .ToListAsync();

            Debug.WriteLine($"Found {favorites.Count} favorite ayas to migrate");

            int migratedCount = 0;
            foreach (var fav in favorites)
            {
                var result = await AddAyaToCollectionAsync(fav.Id, favoritesCollectionId);
                if (result > 0)
                {
                    migratedCount++;
                }
            }

            Debug.WriteLine($"Migrated {migratedCount} favorites to collection");
            return migratedCount;
        }

        #endregion

    }
}
