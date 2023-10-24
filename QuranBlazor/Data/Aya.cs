using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace QuranBlazor.Data
{
    public class Aya
    {
        [PrimaryKey]
        public int Id { get; set; }
        public int SuraId { get; set; }
        public int AyaId { get; set; }
        public string Text { get; set; }
        public string Arabic { get; set; }
        public string Comment { get; set; }
        public string DetailComment { get; set; }
        public bool IsFavorite { get; set; }
        public bool HasNote { get; set; }
    }
}
