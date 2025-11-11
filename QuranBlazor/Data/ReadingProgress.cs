using SQLite;
using System;

namespace QuranBlazor.Data
{
    /// <summary>
    /// Tracks reading progress for each Sura
    /// </summary>
    public class ReadingProgress
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// The Sura ID being tracked
        /// </summary>
        public int SuraId { get; set; }

        /// <summary>
        /// Total number of Ayas in this Sura
        /// </summary>
        public int TotalAyas { get; set; }

        /// <summary>
        /// Number of Ayas marked as read
        /// </summary>
        public int AyasRead { get; set; }

        /// <summary>
        /// Date when the Sura was first opened
        /// </summary>
        public DateTime FirstReadDate { get; set; }

        /// <summary>
        /// Date of the most recent reading activity
        /// </summary>
        public DateTime LastReadDate { get; set; }

        /// <summary>
        /// Whether the entire Sura has been completed
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Date when the Sura was completed (if applicable)
        /// </summary>
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Calculates the completion percentage
        /// </summary>
        [Ignore]
        public double ProgressPercentage => TotalAyas > 0 ? (double)AyasRead / TotalAyas * 100 : 0;
    }

    /// <summary>
    /// Tracks individual Aya read status for detailed progress
    /// </summary>
    public class AyaReadStatus
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to Aya.Id
        /// </summary>
        public int AyaId { get; set; }

        /// <summary>
        /// Sura ID for quick lookups
        /// </summary>
        public int SuraId { get; set; }

        /// <summary>
        /// Aya number within the Sura
        /// </summary>
        public int AyaNumber { get; set; }

        /// <summary>
        /// Whether this Aya has been marked as read
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// Date when the Aya was marked as read
        /// </summary>
        public DateTime? ReadDate { get; set; }

        /// <summary>
        /// Number of times this Aya has been read
        /// </summary>
        public int ReadCount { get; set; }
    }
}
