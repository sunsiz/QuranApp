using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace QuranBlazor.Data
{
    public class Sura
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string ArabicName { get; set;}
        public string Meaning { get; set; }
        public string Description { get; set; }
        public int AyaCount { get; set; }
        public bool RevealedIn { get; set; }

    }
}
