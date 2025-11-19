using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuranBlazor.Data
{
    /// <summary>
    /// View model for displaying favorite Ayas with Sura context
    /// </summary>
    public class FavoritesViewModel
    {
        /// <summary>
        /// Sura number the Aya belongs to
        /// </summary>
        public int SuraId { get; set; }
        
        /// <summary>
        /// Aya number within the Sura
        /// </summary>
        public int AyaId { get; set; }
        
        /// <summary>
        /// Uzbek translation text of the Aya
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// Name of the Sura this Aya belongs to
        /// </summary>
        public string SuraName { get; set; }
    }
}
