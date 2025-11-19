using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace QuranBlazor.Data
{
    /// <summary>
    /// Represents a user-created note for a specific Aya
    /// </summary>
    public class Note
    {
        /// <summary>
        /// Primary key identifier
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// Sura number the note is associated with
        /// </summary>
        public int SuraId { get; set; }
        
        /// <summary>
        /// Aya number the note is associated with
        /// </summary>
        public int AyaId { get; set; }
        
        /// <summary>
        /// Note title (auto-generated based on Sura and Aya)
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// User-entered note content
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }
}
