using Microsoft.Maui.Storage;
using System.Diagnostics;

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
            if (size < 10 || size > 36) 
            {
                Debug.WriteLine($"Invalid Arabic font size: {size}. Must be between 10 and 36.");
                return;
            }
            Preferences.Set("ArabicFontSize", size);
            NotifyFontSizeChanged();
        }

        public static void SetTranslateFontSize(int size)
        {
            if (size < 10 || size > 28) 
            {
                Debug.WriteLine($"Invalid translate font size: {size}. Must be between 10 and 28.");
                return;
            }
            Preferences.Set("TranslateFontSize", size);
            NotifyFontSizeChanged();
        }

        public static void SetCommentFontSize(int size)
        {
            if (size < 8 || size > 24) 
            {
                Debug.WriteLine($"Invalid comment font size: {size}. Must be between 8 and 24.");
                return;
            }
            Preferences.Set("CommentFontSize", size);
            NotifyFontSizeChanged();
        }

        public static void NotifyFontSizeChanged()
        {
            OnFontSizeChanged?.Invoke();
        }
    }
}
