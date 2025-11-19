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
    /// Represents a Sura (chapter) of the Quran
    /// </summary>
    public class Sura
    {
        /// <summary>
        /// Primary key identifier
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// Uzbek name of the Sura
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Arabic name of the Sura
        /// </summary>
        public string ArabicName { get; set; }
        
        /// <summary>
        /// Meaning or translation of the Sura name
        /// </summary>
        public string Meaning { get; set; }
        
        /// <summary>
        /// Description or context of the Sura
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Number of Ayas (verses) in this Sura
        /// </summary>
        public int AyaCount { get; set; }
        
        /// <summary>
        /// True if revealed in Madina, false if revealed in Makka
        /// </summary>
        public bool RevealedIn { get; set; }
    }
}
