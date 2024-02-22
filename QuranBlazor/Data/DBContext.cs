using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using SQLite;

namespace QuranBlazor.Data
{
    public class DBContext
    {
        private readonly string _dbPath;
        SQLiteConnection conn;
        public DBContext(string dbPath)
        {
            _dbPath = dbPath;
        }


/* Unmerged change from project 'QuranBlazor (net7.0-maccatalyst)'
Before:
        private void Initialize()
After:
        private void Task InitializeAsync()
*/
        private async Task InitializeAsync()
        {
            // Don't Create database if it exists
            if (conn != null)
                return;

            try
            {
                if (conn == null)
                {
                    // Create database and Tables
                    if (!File.Exists(_dbPath)) await CopyFileToAppDataDirectory();//CopyDB(_dbPath);
                    conn = new SQLiteConnection(_dbPath,
                        SQLite.SQLiteOpenFlags.ReadWrite | SQLite.SQLiteOpenFlags.Create | SQLite.SQLiteOpenFlags.SharedCache)
                    {
                        Tracer = new Action<string>(q => Debug.WriteLine(q)),
                        Trace = true
                    };
                    Debug.WriteLine("The database path: " + conn.DatabasePath);
                    Debug.WriteLine("The table counts before: " + conn.TableMappings.Count());
                    conn.CreateTable<Sura>();
                    conn.CreateTable<Aya>();
                    conn.CreateTable<Note>();
                    Debug.WriteLine("The table counts after: " + conn.TableMappings.Count());
                    Debug.WriteLine(conn.DatabasePath);
                    //conn.Backup(conn.DatabasePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        //private async void CopyDB(string databasePath)
        //{
        //    try
        //    {
        //        using FileStream outputStream = File.OpenWrite(databasePath);
        //        using Stream fs = await Microsoft.Maui.Storage.FileSystem.Current.OpenAppPackageFileAsync("Quran.db");
        //        using BinaryWriter writer = new BinaryWriter(outputStream);
        //        using (BinaryReader reader = new BinaryReader(fs))
        //        {
        //            var bytesRead = 0;

        //            int bufferSize = 1024;
        //            var buffer = new byte[bufferSize];
        //            using (fs)
        //            {
        //                do
        //                {
        //                    buffer = reader.ReadBytes(bufferSize);
        //                    bytesRead = buffer.Count();
        //                    writer.Write(buffer);
        //                }

        //                while (bytesRead > 0);

        //            }
        //        }
        //    }
        //    catch (Exception exception)
        //    {
        //        Console.WriteLine(exception);
        //    }
        //}

        public async Task CopyFileToAppDataDirectory()
        {
            string filename = @"quran.db";
            // Open the source file
            using Stream inputStream = Microsoft.Maui.Storage.FileSystem.Current.OpenAppPackageFileAsync(filename).Result;

            // Create an output filename
            string targetFile = Path.Combine(Microsoft.Maui.Storage.FileSystem.Current.AppDataDirectory, filename);

            // Copy the file to the AppDataDirectory
            using FileStream outputStream = File.Create(targetFile);
            inputStream.CopyToAsync(outputStream).Wait(TimeSpan.FromSeconds(30));
        }

        public async Task<IEnumerable<Sura>> GetSuraListAsync()
        {
            await InitializeAsync();
            return conn.Table<Sura>().ToList();
        }

        public async Task<IEnumerable<Aya>> GetAyaListAsync(int suraId)
        {
            await InitializeAsync();
            //var tableMapping = conn.Table<Aya>().Table;
            //Debug.WriteLine("Aya table PK is:" + tableMapping.PK);
            return conn.Table<Aya>().Where(a => a.SuraId == suraId).ToList();
        }

        public async Task UpdateAya(Aya aya)
        {
            await InitializeAsync();
            var result = conn.Update(aya);
            Debug.WriteLine(result);
        }

        public Note GetNote(int ayaId, int suraId)
        {
            return conn.Table<Note>().FirstOrDefault(n => n.AyaId == ayaId && n.SuraId == suraId);
        }

        public int AddNote(int ayaId, int suraId, string suraName, string note)
        {
            int result=0;
            try
            {
                var newNote = new Note()
                {
                    AyaId = ayaId,
                    Content = note,
                    SuraId = suraId,
                    Title = suraId + ". Сура " + suraName + ", " + ayaId + ". Оят"
                };
                result = conn.Insert(newNote);
                Debug.WriteLine("Add note result is:" + result);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.Message);
            }
            return result;
        }

        public int DeleteNote(int ayaId, int suraId)
        {
            //Debug.WriteLine("Delete note result is:" +
            //                conn.Table<Note>().Delete(n => n.AyaId == ayaId && n.SuraId == suraId));
            var note = conn.Table<Note>().FirstOrDefault(n => n.SuraId == suraId && n.AyaId == ayaId);
            if (note != null)
            {
                var noteResult = conn.Delete(note);
                Debug.WriteLine("Delete note result is:" + noteResult);
            }

            var aya = conn.Table<Aya>().FirstOrDefault(a => a.SuraId == suraId && a.AyaId == ayaId);
            aya.HasNote = false;
            var result = conn.Update(aya);
            Debug.WriteLine("Delete has note from aya result is:" + result);
            return result;
        }

        public async Task<int> UpdateNote(int ayaId, int suraId)
        {
            int result = -1;
            var note = conn.Table<Note>().FirstOrDefault(n => n.SuraId == suraId && n.AyaId == ayaId);
            if (note != null)
            {
                await InitializeAsync();
                result = conn.Update(note);
                Debug.WriteLine(result);
            }
            return result;
        }

        public async Task<List<Aya>> GetFavorites()
        {
            await InitializeAsync();
            return conn.Table<Aya>().Where(a => a.IsFavorite == true).ToList();
        }

        public int DeleteFavorite(int ayaId, int suraId)
        {
            var aya = conn.Table<Aya>().FirstOrDefault(a => a.SuraId == suraId && a.AyaId == ayaId);
            aya.IsFavorite = false;
            var result = conn.Update(aya);
            Debug.WriteLine("Delete favorite aya result is:" + result);
            return result;
        }

        public async Task<string> GetSuraNameAsync(int suraId)
        {
            if (suraId == 0) return "";
            await InitializeAsync();
            return conn.Table<Sura>().FirstOrDefault(s => s.Id == suraId).Name;
        }

        public async Task<List<Note>> GetNotes()
        {
            await InitializeAsync();
            return conn.Table<Note>().ToList();
        }

        public async Task<IEnumerable<Aya>> GetSearchResultAsync(string keyWord)
        {
            await InitializeAsync();
            if (string.IsNullOrEmpty(keyWord)) return null;
            return conn.Table<Aya>().Where(a => a.Text.Contains(keyWord) || a.Comment.Contains(keyWord)).ToList();
        }
    }
}
