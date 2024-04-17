using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuranBlazor.Data
{
    public class FavoritesViewModel
    {
        public int SuraId { get; set; }
        public int AyaId { get; set; }
        public string Text { get; set; }
        public string SuraName { get; set; }
    }
}
