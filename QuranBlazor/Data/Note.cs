using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuranBlazor.Data
{
    public class Note
    {
        public int SuraId { get; set; }
        public int AyaId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
