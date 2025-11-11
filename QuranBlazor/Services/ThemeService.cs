namespace QuranBlazor.Services
{
    /// <summary>
    /// Service for managing application theme (Light/Dark mode)
    /// </summary>
    public static class ThemeService
    {
        private static readonly object _lock = new object();
        public static event Action OnThemeChanged;

        public static void NotifyThemeChanged()
        {
            lock (_lock)
            {
                OnThemeChanged?.Invoke();
            }
        }

        /// <summary>
        /// Gets the current theme ("Light" or "Dark")
        /// </summary>
        public static string CurrentTheme
        {
            get => Preferences.Get(PreferenceKeys.Theme, PreferenceKeys.DefaultTheme);
            set
            {
                if (CurrentTheme != value)
                {
                    Preferences.Set(PreferenceKeys.Theme, value);
                    NotifyThemeChanged();
                }
            }
        }

        /// <summary>
        /// Checks if dark mode is currently active
        /// </summary>
        public static bool IsDarkMode => CurrentTheme == "Dark";

        /// <summary>
        /// Toggles between light and dark themes
        /// </summary>
        public static void ToggleTheme()
        {
            CurrentTheme = IsDarkMode ? "Light" : "Dark";
        }

        /// <summary>
        /// Initializes theme based on system preference if not set
        /// </summary>
        public static void InitializeTheme()
        {
            if (!Preferences.ContainsKey(PreferenceKeys.Theme))
            {
                // Check system theme preference
                var systemTheme = Application.Current?.RequestedTheme ?? AppTheme.Light;
                CurrentTheme = systemTheme == AppTheme.Dark ? "Dark" : "Light";
            }
        }

        /// <summary>
        /// Gets CSS class for current theme
        /// </summary>
        public static string ThemeClass => IsDarkMode ? "dark-theme" : "light-theme";

        /// <summary>
        /// Gets appropriate icon name for current theme
        /// </summary>
        /// <param name="lightIcon">Icon name for light theme</param>
        /// <param name="darkIcon">Icon name for dark theme</param>
        public static string GetThemedIcon(string lightIcon, string darkIcon)
        {
            return IsDarkMode ? darkIcon : lightIcon;
        }
    }
}
