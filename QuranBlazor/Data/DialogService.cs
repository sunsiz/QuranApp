using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace QuranBlazor.Data
{
    internal class DialogService
    {
        public async Task<bool> DisplayConfirm(string title, string message, string accept, string cancel)
        {
            return await (Application.Current?.MainPage.DisplayAlert(title, message, accept, cancel)).ConfigureAwait(false);
        }
        public async Task<string> DisplayPrompt(string title, string message, string accept, string cancel)
        {
            if (title == "Оятга ўтиш")
            {
                return await Application.Current.MainPage
                    .DisplayPromptAsync(title, message, accept, cancel, "16:90", 6, null, "16/90")
                    .ConfigureAwait(false);
            }
            else
            {
                return await Application.Current.MainPage
                    .DisplayPromptAsync(title, message, accept, cancel, "", 10240, null, "")
                    .ConfigureAwait(false);
            }
        }
        public async Task DisplayAlert(string title, string message, string cancel)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, cancel).ConfigureAwait(false);
        }
        public async Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
        {
            return await Application.Current.MainPage.DisplayAlert(title, message, accept, cancel).ConfigureAwait(false);
        }
        public async void DisplayToast(string message)
        {
            ToastDuration duration = ToastDuration.Short;
            double fontSize = Preferences.Get("TranslateFontSize", 14);

            var toast = Toast.Make(message, duration, fontSize);
            
            await toast.Show().ConfigureAwait(false);
        }

        public async void DisplaySnackbar(string message)
        {
            var snackbar = Snackbar.Make(message);
            await snackbar.Show().ConfigureAwait(false);
        }
    }
}
