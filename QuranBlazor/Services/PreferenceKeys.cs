namespace QuranBlazor.Services
{
    /// <summary>
    /// Centralized preference keys used throughout the application.
    /// Prevents magic strings and ensures consistency.
    /// </summary>
    public static class PreferenceKeys
    {
        /// <summary>
        /// Key for storing the last bookmark position (format: "SuraId:AyaId")
        /// </summary>
        public const string Bookmark = "Bookmark";

        /// <summary>
        /// Key for storing the selected Uzbek script ("Cyrillic" or "Latin")
        /// </summary>
        public const string UzbekScript = "UzbekScript";

        /// <summary>
        /// Key for storing the Arabic text font size (10-36)
        /// </summary>
        public const string ArabicFontSize = "ArabicFontSize";

        /// <summary>
        /// Key for storing the translation text font size (12-28)
        /// </summary>
        public const string TranslateFontSize = "TranslateFontSize";

        /// <summary>
        /// Key for storing the comment text font size (6-24)
        /// </summary>
        public const string CommentFontSize = "CommentFontSize";

        /// <summary>
        /// Key for storing the theme preference ("Light" or "Dark")
        /// </summary>
        public const string Theme = "Theme";

        // Default values
        public const string DefaultBookmark = "49:11";
        public const string DefaultScript = "Cyrillic";
        public const int DefaultArabicFontSize = 16;
        public const int DefaultTranslateFontSize = 14;
        public const int DefaultCommentFontSize = 12;
        public const string DefaultTheme = "Light";
    }
}
