using Microsoft.Maui.Storage;

namespace QuranBlazor.Services
{
    /// <summary>
    /// Service to manage search history
    /// </summary>
    public static class SearchHistoryService
    {
        private const string SearchHistoryKey = "SearchHistory";
        private const int MaxHistoryItems = 10;
        private const char Separator = '|';

        public static List<string> GetHistory()
        {
            var stored = Preferences.Get(SearchHistoryKey, string.Empty);
            if (string.IsNullOrEmpty(stored))
                return new List<string>();

            return stored.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
        }

        public static void AddSearch(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return;

            keyword = keyword.Trim();
            var history = GetHistory();

            // Remove if already exists (to move to front)
            history.Remove(keyword);

            // Add to front
            history.Insert(0, keyword);

            // Keep only MaxHistoryItems
            if (history.Count > MaxHistoryItems)
                history = history.Take(MaxHistoryItems).ToList();

            // Save
            var joined = string.Join(Separator.ToString(), history);
            Preferences.Set(SearchHistoryKey, joined);
        }

        public static void ClearHistory()
        {
            Preferences.Remove(SearchHistoryKey);
        }

        public static void RemoveItem(string keyword)
        {
            var history = GetHistory();
            history.Remove(keyword);
            var joined = string.Join(Separator.ToString(), history);
            Preferences.Set(SearchHistoryKey, joined);
        }
    }
}
