using System.Globalization;
using QuranBlazor.Services;

namespace QuranBlazor
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            SetDefaultScriptOnFirstLaunch();
            ThemeService.InitializeTheme();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            return new Window(new MainPage());
        }

        private static void SetDefaultScriptOnFirstLaunch()
        {
            // If the UzbekScript preference doesn't exist, it's the first launch
            // or preferences have been cleared.
            if (!Preferences.ContainsKey(PreferenceKeys.UzbekScript))
            {
                var currentDeviceCulture = CultureInfo.CurrentUICulture;
                string defaultScript = PreferenceKeys.DefaultScript; // Default to Cyrillic if specific Uzbek script not found

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

                Preferences.Set(PreferenceKeys.UzbekScript, defaultScript);
            }
        }
    }
}