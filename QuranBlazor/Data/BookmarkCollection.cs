using SQLite;
using System;

namespace QuranBlazor.Data
{
    /// <summary>
    /// Represents a collection of bookmarked Ayas organized by user-defined categories
    /// </summary>
    public class BookmarkCollection
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Name of the collection (e.g., "Favorite Duas", "Important Verses")
        /// </summary>
        [Collation("NOCASE")]
        public string Name { get; set; }

        /// <summary>
        /// Optional description of the collection
        /// </summary>
        [Collation("NOCASE")]
        public string Description { get; set; }

        /// <summary>
        /// Date the collection was created
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Display order for sorting collections
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Color code for visual identification (e.g., "#FF5733")
        /// </summary>
        public string ColorCode { get; set; }
    }

    /// <summary>
    /// Junction table linking Ayas to BookmarkCollections (many-to-many relationship)
    /// </summary>
    public class AyaBookmarkCollection
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to Aya.Id
        /// </summary>
        public int AyaId { get; set; }

        /// <summary>
        /// Foreign key to BookmarkCollection.Id
        /// </summary>
        public int CollectionId { get; set; }

        /// <summary>
        /// Date the Aya was added to this collection
        /// </summary>
        public DateTime AddedDate { get; set; }

        /// <summary>
        /// Optional notes specific to this Aya in this collection
        /// </summary>
        [Collation("NOCASE")]
        public string Notes { get; set; }
    }
}
