using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using QuranBlazor.Services;

namespace QuranBlazor.Data
{
    internal class DialogService
    {
        private Page GetMainPage()
        {
            // Try to get the main page safely
            var mainPage = Application.Current?.Windows?.FirstOrDefault()?.Page;
            if (mainPage == null)
            {
                throw new InvalidOperationException("Unable to access the main page. The application may not be fully initialized.");
            }
            return mainPage;
        }

        public async Task<bool> DisplayConfirm(string title, string message, string accept, string cancel)
        {
            try
            {
                var page = GetMainPage();
                return await page.DisplayAlert(title, message, accept, cancel).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error displaying confirm dialog: {ex.Message}");
                return false;
            }
        }

        public async Task<string> DisplayPrompt(string title, string message, string accept, string cancel)
        {
            try
            {
                var page = GetMainPage();
                if (title == TransliterationService.GetDisplayText("Оятга ўтиш"))
                {
                    return await page.DisplayPromptAsync(title, message, accept, cancel, "16:90 ёки 16/90", 6, null, "").ConfigureAwait(false);
                }
                else
                {
                    return await page.DisplayPromptAsync(title, message, accept, cancel, "", 10240, null, "").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error displaying prompt dialog: {ex.Message}");
                return null;
            }
        }

        public async Task DisplayAlert(string title, string message, string cancel)
        {
            try
            {
                var page = GetMainPage();
                await page.DisplayAlert(title, message, cancel).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error displaying alert: {ex.Message}");
            }
        }

        public async Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
        {
            try
            {
                var page = GetMainPage();
                return await page.DisplayAlert(title, message, accept, cancel).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error displaying alert: {ex.Message}");
                return false;
            }
        }

        public async void DisplayToast(string message)
        {
            try
            {
                ToastDuration duration = ToastDuration.Short;
                double fontSize = Preferences.Get(PreferenceKeys.TranslateFontSize, PreferenceKeys.DefaultTranslateFontSize);

                var toast = Toast.Make(message, duration, fontSize);

                await toast.Show().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error displaying toast: {ex.Message}");
            }
        }

        public async void DisplaySnackbar(string message)
        {
            try
            {
                var snackbar = Snackbar.Make(message);
                await snackbar.Show().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error displaying snackbar: {ex.Message}");
            }
        }
    }
}
