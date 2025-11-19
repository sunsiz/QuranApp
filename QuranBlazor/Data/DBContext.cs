using SQLite;
using System.Diagnostics;
using Microsoft.Data.Sqlite;

namespace QuranBlazor.Data
{
    public class DbContext
    {
        private readonly string _dbPath;
        private SQLiteAsyncConnection _conn;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private volatile bool _initialized = false;

        public DbContext(string dbPath)
        {
            _dbPath = dbPath;
            // Don't block here - initialize lazily on first use
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;

            await _initLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_initialized)
                {
                    await InitializeAsync().ConfigureAwait(false);
                    _initialized = true;
                }
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
                sourceStream = await Microsoft.Maui.Storage.FileSystem.Current.OpenAppPackageFileAsync(filename).ConfigureAwait(false);
#endif
                using (sourceStream)
                {
                    using var targetStream = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write);
                    await sourceStream.CopyToAsync(targetStream).ConfigureAwait(false);
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
            await EnsureInitializedAsync().ConfigureAwait(false);
            return await _conn.Table<Sura>().ToListAsync().ConfigureAwait(false);
        }

        public async Task<List<Aya>> GetAyaListAsync(int suraId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            return await _conn.Table<Aya>().Where(a => a.SuraId == suraId).ToListAsync().ConfigureAwait(false);
        }

        public async Task<Aya> GetAyaAsync(int suraId, int ayaId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            return await _conn.Table<Aya>()
                .FirstOrDefaultAsync(a => a.SuraId == suraId && a.AyaId == ayaId)
                .ConfigureAwait(false);
        }

        public async Task<int> UpdateAyaAsync(Aya aya)
        {
            if (aya == null) return -1;
            
            await EnsureInitializedAsync().ConfigureAwait(false);
            var result = await _conn.UpdateAsync(aya).ConfigureAwait(false);
            Debug.WriteLine($"UpdateAya result: {result}");
            return result;
        }

        public async Task<Note> GetNoteAsync(int ayaId, int suraId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            return await _conn.Table<Note>().FirstOrDefaultAsync(n => n.AyaId == ayaId && n.SuraId == suraId).ConfigureAwait(false);
        }

        public async Task<int> AddNoteAsync(int ayaId, int suraId, string suraName, string note)
        {
            if (string.IsNullOrWhiteSpace(note)) return -1;
            
            await EnsureInitializedAsync().ConfigureAwait(false);
            var newNote = new Note
            {
                AyaId = ayaId,
                Content = note.Trim(),
                SuraId = suraId,
                Title = $"{suraId}. Сура {suraName ?? ""}, {ayaId}. Оят"
            };
            
            var result = await _conn.InsertAsync(newNote).ConfigureAwait(false);
            
            if (result > 0)
            {
                await _conn.ExecuteAsync(
                    "UPDATE Aya SET HasNote = 1 WHERE SuraId = ? AND AyaId = ?",
                    suraId, ayaId).ConfigureAwait(false);
            }

            Debug.WriteLine("Add note result is:" + result);
            return result;
        }

        public async Task<int> DeleteNoteAsync(int ayaId, int suraId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            var noteResult = await _conn.Table<Note>().DeleteAsync(n => n.SuraId == suraId && n.AyaId == ayaId).ConfigureAwait(false);
            Debug.WriteLine("Delete note result is:" + noteResult);

            if (noteResult > 0)
            {
                await _conn.ExecuteAsync(
                    "UPDATE Aya SET HasNote = 0 WHERE SuraId = ? AND AyaId = ?",
                    suraId, ayaId).ConfigureAwait(false);
            }

            return noteResult;
        }

        public async Task<int> UpdateNoteAsync(Note note)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            if (note == null) return -1;
            var result = await _conn.UpdateAsync(note).ConfigureAwait(false);
            Debug.WriteLine($"Update note result: {result}");
            return result;
        }

        public async Task<List<FavoritesViewModel>> GetFavoritesAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            var favoriteAyas = await _conn.Table<Aya>().Where(a => a.IsFavorite).ToListAsync().ConfigureAwait(false);
            
            // Ensure cache is populated
            await EnsureSuraNameCacheAsync().ConfigureAwait(false);

            var favorites = favoriteAyas.Select(a => new FavoritesViewModel
            {
                SuraId = a.SuraId,
                AyaId = a.AyaId,
                Text = a.Text,
                SuraName = _suraNameCache.TryGetValue(a.SuraId, out var name) ? name : string.Empty
            }).ToList();

            return favorites;
        }

        public async Task<int> DeleteFavoriteAsync(int ayaId, int suraId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            var aya = await _conn.Table<Aya>().FirstOrDefaultAsync(a => a.SuraId == suraId && a.AyaId == ayaId).ConfigureAwait(false);
            aya.IsFavorite = false;
            var result = await _conn.UpdateAsync(aya).ConfigureAwait(false);
            Debug.WriteLine("Delete favorite aya result is:" + result);
            return result;
        }

        private Dictionary<int, string> _suraNameCache;
        private List<BookmarkCollection> _bookmarkCollectionsCache;
        private readonly SemaphoreSlim _bookmarkCacheLock = new(1, 1);
        private readonly SemaphoreSlim _suraNameCacheLock = new(1, 1);

        public async Task<string> GetSuraNameAsync(int suraId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            await EnsureSuraNameCacheAsync().ConfigureAwait(false);
            return _suraNameCache.TryGetValue(suraId, out var name) ? name : string.Empty;
        }


        public async Task<List<Note>> GetNotesAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            return await _conn.Table<Note>().ToListAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// This method is used to search for Ayas (verses of the Quran) that contain a specific keyword in their text or comment.
        /// Because SQLite does not support the Unicode character comparison well (can't perform case insensitive comparison on Cyrillic characters), we have to create a custom function to compare the text and the keyword.
        /// </summary>
        /// <param name="keyWord">The keyword is passed as a parameter to the method.</param>
        /// <returns>The list of Ayas that matches th condition</returns>
        public async Task<IEnumerable<Aya>> GetSearchResult(string keyWord)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(keyWord)) return Array.Empty<Aya>();

            var ayas = new List<Aya>();
            using var connection = new SqliteConnection("Data Source=" + _dbPath);

            connection.CreateFunction(
                "ContainsKeyword",
                (string fieldValue, string keyword) => fieldValue != null && keyword != null && fieldValue.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            );

            await connection.OpenAsync().ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = @"SELECT * FROM Aya WHERE ContainsKeyword(Text, $kw) or ContainsKeyword(Comment, $kw);";
            command.Parameters.AddWithValue("$kw", keyWord);

            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var aya = new Aya
                {
                    Id = reader.GetInt32(0),
                    SuraId = reader.GetInt32(1),
                    AyaId = reader.GetInt32(2),
                    Text = reader.GetString(3),
                    Arabic = reader.GetString(4),
                    Comment = reader.IsDBNull(5) ? null : reader.GetString(5),
                    DetailComment = reader.IsDBNull(6) ? null : reader.GetString(6),
                    IsFavorite = !reader.IsDBNull(7) && reader.GetBoolean(7),
                    HasNote = !reader.IsDBNull(8) && reader.GetBoolean(8)
                };
                ayas.Add(aya);
            }

            return ayas;
        }

        private async Task EnsureSuraNameCacheAsync()
        {
            if (_suraNameCache != null) return;
            
            await _suraNameCacheLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_suraNameCache == null)
                {
                    var suras = await _conn.Table<Sura>().ToListAsync().ConfigureAwait(false);
                    _suraNameCache = suras.ToDictionary(s => s.Id, s => s.Name);
                }
            }
            finally
            {
                _suraNameCacheLock.Release();
            }
        }

        private void InvalidateBookmarkCollectionsCache()
        {
            _bookmarkCollectionsCache = null;
        }

        #region Bookmark Collections

        /// <summary>
        /// Get all bookmark collections
        /// </summary>
        public async Task<List<BookmarkCollection>> GetBookmarkCollectionsAsync(bool forceRefresh = false)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            await _bookmarkCacheLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (forceRefresh || _bookmarkCollectionsCache == null)
                {
                    _bookmarkCollectionsCache = await _conn.Table<BookmarkCollection>()
                        .OrderBy(c => c.DisplayOrder)
                        .ToListAsync()
                        .ConfigureAwait(false);
                }

                return _bookmarkCollectionsCache
                    .Select(collection => collection.Clone())
                    .ToList();
            }
            finally
            {
                _bookmarkCacheLock.Release();
            }
        }

        /// <summary>
        /// Get a specific bookmark collection by ID
        /// </summary>
        public async Task<BookmarkCollection> GetBookmarkCollectionAsync(int collectionId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            var collection = await _conn.Table<BookmarkCollection>()
                .FirstOrDefaultAsync(c => c.Id == collectionId)
                .ConfigureAwait(false);
            return collection?.Clone();
        }

        /// <summary>
        /// Create a new bookmark collection
        /// </summary>
        public async Task<int> AddBookmarkCollectionAsync(BookmarkCollection collection)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            if (collection == null) return -1;
            collection.Name = collection.Name?.Trim();
            collection.Description = collection.Description?.Trim();
            if (string.IsNullOrWhiteSpace(collection.Name)) return -1;
            
            var existing = await _conn.Table<BookmarkCollection>()
                .Where(c => c.Name == collection.Name)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            
            if (existing != null)
            {
                Debug.WriteLine($"Collection with name '{collection.Name}' already exists");
                return -1; // Or return existing.Id if you want to reuse
            }
            
            collection.CreatedDate = DateTime.Now;
            
            // Set display order to max + 1 if not specified
            if (collection.DisplayOrder == 0)
            {
                var maxOrder = await _conn.ExecuteScalarAsync<int>("SELECT IFNULL(MAX(DisplayOrder),0) FROM BookmarkCollection").ConfigureAwait(false);
                collection.DisplayOrder = maxOrder + 1;
            }
            
            var result = await _conn.InsertAsync(collection).ConfigureAwait(false);
            Debug.WriteLine($"Add bookmark collection result: {result}");
            InvalidateBookmarkCollectionsCache();
            return collection.Id;
        }

        /// <summary>
        /// Update a bookmark collection
        /// </summary>
        public async Task<int> UpdateBookmarkCollectionAsync(BookmarkCollection collection)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            if (collection == null) return -1;
            var result = await _conn.UpdateAsync(collection).ConfigureAwait(false);
            Debug.WriteLine($"Update bookmark collection result: {result}");
            InvalidateBookmarkCollectionsCache();
            return result;
        }

        /// <summary>
        /// Delete a bookmark collection and all its associations
        /// </summary>
        public async Task<int> DeleteBookmarkCollectionAsync(int collectionId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            // Delete all associations first
            await _conn.Table<AyaBookmarkCollection>().DeleteAsync(abc => abc.CollectionId == collectionId).ConfigureAwait(false);
            
            var result = await _conn.Table<BookmarkCollection>().DeleteAsync(c => c.Id == collectionId).ConfigureAwait(false);
            Debug.WriteLine($"Delete bookmark collection result: {result}");
            InvalidateBookmarkCollectionsCache();
            return result;
        }

        /// <summary>
        /// Remove duplicate collections (keep the oldest one for each name)
        /// </summary>
        public async Task<int> RemoveDuplicateCollectionsAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            
            var allCollections = await _conn.Table<BookmarkCollection>().ToListAsync().ConfigureAwait(false);
            var duplicates = allCollections
                .GroupBy(c => c.Name.ToLower())
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.OrderBy(c => c.CreatedDate).Skip(1)) // Keep first (oldest), remove rest
                .ToList();
            
            int deletedCount = 0;
            foreach (var duplicate in duplicates)
            {
                await DeleteBookmarkCollectionAsync(duplicate.Id);
                deletedCount++;
            }
            
            Debug.WriteLine($"Removed {deletedCount} duplicate collections");
            if (deletedCount > 0)
            {
                InvalidateBookmarkCollectionsCache();
            }
            return deletedCount;
        }

        /// <summary>
        /// Add an Aya to a bookmark collection
        /// </summary>
        public async Task<int> AddAyaToCollectionAsync(int ayaId, int collectionId, string notes = null)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            // Check if already exists
            var existing = await _conn.Table<AyaBookmarkCollection>()
                .FirstOrDefaultAsync(abc => abc.AyaId == ayaId && abc.CollectionId == collectionId)
                .ConfigureAwait(false);
            
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

            var result = await _conn.InsertAsync(newAssociation).ConfigureAwait(false);
            Debug.WriteLine($"Add Aya to collection result: {result}");
            return result;
        }

        /// <summary>
        /// Remove an Aya from a bookmark collection
        /// </summary>
        public async Task<int> RemoveAyaFromCollectionAsync(int ayaId, int collectionId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            var result = await _conn.Table<AyaBookmarkCollection>()
                .DeleteAsync(abc => abc.AyaId == ayaId && abc.CollectionId == collectionId)
                .ConfigureAwait(false);
            Debug.WriteLine($"Remove Aya from collection result: {result}");
            return result;
        }

        /// <summary>
        /// Get all Ayas in a specific bookmark collection
        /// </summary>
        public async Task<List<FavoritesViewModel>> GetAyasInCollectionAsync(int collectionId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            await EnsureSuraNameCacheAsync().ConfigureAwait(false);
            var associations = await _conn.Table<AyaBookmarkCollection>()
                .Where(abc => abc.CollectionId == collectionId)
                .ToListAsync()
                .ConfigureAwait(false);

            if (!associations.Any())
            {
                return new List<FavoritesViewModel>();
            }

            var ayaIds = associations.Select(a => a.AyaId).ToList();
            var ayas = await _conn.Table<Aya>()
                .Where(a => ayaIds.Contains(a.Id))
                .ToListAsync()
                .ConfigureAwait(false);

            var result = ayas.Select(a =>
            {
                return new FavoritesViewModel
                {
                    SuraId = a.SuraId,
                    AyaId = a.AyaId,
                    Text = a.Text,
                    SuraName = _suraNameCache.TryGetValue(a.SuraId, out var name) ? name : string.Empty
                };
            }).ToList();

            return result;
        }

        /// <summary>
        /// Get all collections that contain a specific Aya
        /// </summary>
        public async Task<List<BookmarkCollection>> GetCollectionsForAyaAsync(int suraId, int ayaId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            var aya = await GetAyaAsync(suraId, ayaId).ConfigureAwait(false);
            if (aya == null) return new List<BookmarkCollection>();

            var associations = await _conn.Table<AyaBookmarkCollection>()
                .Where(abc => abc.AyaId == aya.Id)
                .ToListAsync()
                .ConfigureAwait(false);

            if (!associations.Any())
            {
                return new List<BookmarkCollection>();
            }

            var lookup = associations.Select(a => a.CollectionId).ToHashSet();
            var allCollections = await GetBookmarkCollectionsAsync().ConfigureAwait(false);

            return allCollections
                .Where(c => lookup.Contains(c.Id))
                .OrderBy(c => c.DisplayOrder)
                .ToList();
        }

        /// <summary>
        /// Get count of Ayas in a collection
        /// </summary>
        public async Task<int> GetCollectionAyaCountAsync(int collectionId)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            return await _conn.Table<AyaBookmarkCollection>()
                .Where(abc => abc.CollectionId == collectionId)
                .CountAsync()
                .ConfigureAwait(false);
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
            await EnsureInitializedAsync().ConfigureAwait(false);
            
            // Check if favorites collection already exists (by name)
            var existing = await _conn.Table<BookmarkCollection>()
                .FirstOrDefaultAsync(c => c.Name == "❤️ Белгиланган оятлар" || c.Name == "❤️ Belgilangan oyatlar")
                .ConfigureAwait(false);
            
            if (existing != null)
            {
                Debug.WriteLine($"Favorites collection already exists with ID: {existing.Id}");
                return existing.Id;
            }

            var favoritesCollection = new BookmarkCollection
            {
                Name = "❤️ Белгиланган оятлар",
                Description = "Севимли оятларингиз",
                CreatedDate = DateTime.Now,
                DisplayOrder = 0,
                ColorCode = "#DC143C" // Crimson red for favorites
            };

            var result = await _conn.InsertAsync(favoritesCollection).ConfigureAwait(false);
            Debug.WriteLine($"Created default favorites collection with ID: {favoritesCollection.Id}");
            InvalidateBookmarkCollectionsCache();
            
            // Create sample collections for new users
            await CreateSampleCollectionsAsync().ConfigureAwait(false);
            
            return favoritesCollection.Id;
        }

        /// <summary>
        /// Create sample bookmark collections to help new users understand the feature.
        /// Only creates them once - checks database directly to prevent duplicates.
        /// </summary>
        private async Task CreateSampleCollectionsAsync()
        {
            var sampleCollections = new[]
            {
                new BookmarkCollection
                {
                    Name = "📿 Дуолар",
                    Description = "Муҳим дуолар ва зикрлар",
                    CreatedDate = DateTime.Now,
                    DisplayOrder = 1,
                    ColorCode = "#4CAF50" // Green
                },
                new BookmarkCollection
                {
                    Name = "💡 Ҳикматли оятлар",
                    Description = "Ҳаёт учун ҳикматлар",
                    CreatedDate = DateTime.Now,
                    DisplayOrder = 3,
                    ColorCode = "#2196F3" // Blue
                },
                new BookmarkCollection
                {
                    Name = "🕌 Намоз ва ибодат",
                    Description = "Намоз ва ибодат ҳақида оятлар",
                    CreatedDate = DateTime.Now,
                    DisplayOrder = 4,
                    ColorCode = "#9C27B0" // Purple
                }
            };

            int createdCount = 0;
            foreach (var collection in sampleCollections)
            {
                // Check if collection with this name already exists
                var existing = await _conn.Table<BookmarkCollection>()
                    .Where(c => c.Name.ToLower() == collection.Name.ToLower())
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
                
                if (existing == null)
                {
                    await _conn.InsertAsync(collection).ConfigureAwait(false);
                    createdCount++;
                    Debug.WriteLine($"Created sample collection: {collection.Name}");
                }
                else
                {
                    Debug.WriteLine($"Sample collection already exists: {collection.Name}");
                }
            }
            
            Debug.WriteLine($"Created {createdCount} new sample collections (skipped {sampleCollections.Length - createdCount} existing)");
            if (createdCount > 0)
            {
                InvalidateBookmarkCollectionsCache();
            }
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
