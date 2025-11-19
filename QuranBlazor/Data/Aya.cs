using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace QuranBlazor.Data
{
    /// <summary>
    /// Represents an Aya (verse) of the Quran
    /// </summary>
    public class Aya
    {
        /// <summary>
        /// Primary key identifier (database auto-increment)
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// Sura (chapter) number this Aya belongs to
        /// </summary>
        public int SuraId { get; set; }
        
        /// <summary>
        /// Aya (verse) number within the Sura
        /// </summary>
        public int AyaId { get; set; }
        
        /// <summary>
        /// Uzbek translation text (case-insensitive collation)
        /// </summary>
        [Collation("NOCASE")]
        public string Text { get; set; }
        
        /// <summary>
        /// Original Arabic text
        /// </summary>
        public string Arabic { get; set; }
        
        /// <summary>
        /// Brief commentary or footnote (case-insensitive collation)
        /// </summary>
        [Collation("NOCASE")]
        public string Comment { get; set; }
        
        /// <summary>
        /// Detailed commentary or explanation (case-insensitive collation)
        /// </summary>
        [Collation("NOCASE")]
        public string DetailComment { get; set; }
        
        /// <summary>
        /// True if this Aya is marked as favorite by the user
        /// </summary>
        public bool IsFavorite { get; set; }
        
        /// <summary>
        /// True if the user has added a note for this Aya
        /// </summary>
        public bool HasNote { get; set; }
    }
}
