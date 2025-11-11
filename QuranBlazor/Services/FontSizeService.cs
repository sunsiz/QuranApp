using Microsoft.Maui.Storage;

namespace QuranBlazor.Services
{
    /// <summary>
    /// Service to manage font sizes across the application with reactive updates
    /// </summary>
    public static class FontSizeService
    {
        public static event Action OnFontSizeChanged;

        public static int GetArabicFontSize() => Preferences.Get("ArabicFontSize", 16);
        public static int GetTranslateFontSize() => Preferences.Get("TranslateFontSize", 14);
        public static int GetCommentFontSize() => Preferences.Get("CommentFontSize", 12);

        public static void SetArabicFontSize(int size)
        {
            if (size < 12 || size > 32) return;
            Preferences.Set("ArabicFontSize", size);
            NotifyFontSizeChanged();
        }

        public static void SetTranslateFontSize(int size)
        {
            if (size < 12 || size > 24) return;
            Preferences.Set("TranslateFontSize", size);
            NotifyFontSizeChanged();
        }

        public static void SetCommentFontSize(int size)
        {
            if (size < 10 || size > 20) return;
            Preferences.Set("CommentFontSize", size);
            NotifyFontSizeChanged();
        }

        public static void NotifyFontSizeChanged()
        {
            OnFontSizeChanged?.Invoke();
        }
    }
}
