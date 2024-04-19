using SQLite;
using System.Diagnostics;
using Microsoft.Data.Sqlite;

namespace QuranBlazor.Data
{
    public class DbContext
    {
        private readonly string _dbPath;
        private SQLiteAsyncConnection _conn;
        public DbContext(string dbPath)
        {
            _dbPath = dbPath;
            InitializeAsync().Wait();
        }

        private async Task InitializeAsync()
        {
            // Don't Create database if it exists
            if (_conn != null)
                return;

            // Create database and Tables
            if (!File.Exists(_dbPath)) await CopyFileToAppDataDirectory();
            _conn = new SQLiteAsyncConnection(_dbPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

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

        public Task<List<Sura>> GetSuraListAsync()
        {
            return _conn.Table<Sura>().ToListAsync();
        }

        public Task<List<Aya>> GetAyaListAsync(int suraId)
        {
            return _conn.Table<Aya>().Where(a => a.SuraId == suraId).ToListAsync();
        }

        public Task<int> UpdateAyaAsync(Aya aya)
        {
            var result = _conn.UpdateAsync(aya);
            Debug.WriteLine(result);
            return result;
        }

        public Task<Note> GetNoteAsync(int ayaId, int suraId)
        {
            return _conn.Table<Note>().FirstOrDefaultAsync(n => n.AyaId == ayaId && n.SuraId == suraId);
        }

        public async Task<int> AddNoteAsync(int ayaId, int suraId, string suraName, string note)
        {
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
            int result = -1;
            var note = await _conn.Table<Note>().FirstOrDefaultAsync(n => n.SuraId == suraId && n.AyaId == ayaId);
            if (note != null)
            {
                result = await _conn.UpdateAsync(note);
                Debug.WriteLine(result);
            }
            return result;
        }

        public async Task<List<FavoritesViewModel>> GetFavoritesAsync()
        {
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
            var aya = await _conn.Table<Aya>().FirstOrDefaultAsync(a => a.SuraId == suraId && a.AyaId == ayaId);
            aya.IsFavorite = false;
            var result = await _conn.UpdateAsync(aya);
            Debug.WriteLine("Delete favorite aya result is:" + result);
            return result;
        }

        public async Task<string> GetSuraNameAsync(int suraId)
        {
            var sura = await _conn.Table<Sura>().FirstOrDefaultAsync(s => s.Id == suraId);
            return sura?.Name ?? string.Empty;
        }

        public Task<List<Note>> GetNotesAsync()
        {
            return _conn.Table<Note>().ToListAsync();
        }

        /// <summary>
        /// This method is used to search for Ayas (verses of the Quran) that contain a specific keyword in their text or comment.
        /// Because SQLite does not support the Unicode character comparison well (can't perform case insensitive comparison on Cyrillic characters), we have to create a custom function to compare the text and the keyword.
        /// </summary>
        /// <param name="keyWord">The keyword is passed as a parameter to the method.</param>
        /// <returns>The list of Ayas that matches th condition</returns>
        public IEnumerable<Aya> GetSearchResult(string keyWord)
        {
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

    }
}
