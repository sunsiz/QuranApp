using System.Globalization;

namespace QuranBlazor
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            SetDefaultScriptOnFirstLaunch();
            MainPage = new MainPage();
        }

        private static void SetDefaultScriptOnFirstLaunch()
        {
            const string uzbekScriptKey = "UzbekScript";
            // If the UzbekScript preference doesn't exist, it's the first launch
            // or preferences have been cleared.
            if (!Preferences.ContainsKey(uzbekScriptKey))
            {
                var currentDeviceCulture = CultureInfo.CurrentUICulture;
                string defaultScript = "Cyrillic"; // Default to Cyrillic if specific Uzbek script not found

                // Check for Uzbek Latin (e.g., "uz-Latn", "uz-Latn-UZ")
                if (currentDeviceCulture.Name.StartsWith("uz-Latn", StringComparison.OrdinalIgnoreCase))
                {
                    defaultScript = "Latin";
                }
                // Check for Uzbek Cyrillic (e.g., "uz-Cyrl", "uz-Cyrl-UZ")
                else if (currentDeviceCulture.Name.StartsWith("uz-Cyrl", StringComparison.OrdinalIgnoreCase))
                {
                    defaultScript = "Cyrillic";
                }
                // Optional: If just "uz" is found, you might default to one or the other
                else if (currentDeviceCulture.Name.StartsWith("uz", StringComparison.OrdinalIgnoreCase))
                {
                    defaultScript = "Latin"; // Or your preferred default for generic Uzbek
                }

                Preferences.Set(uzbekScriptKey, defaultScript);
            }
        }
    }
}