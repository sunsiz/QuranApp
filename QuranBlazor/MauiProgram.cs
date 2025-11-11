using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using QuranBlazor.Data;
using QuranBlazor.Services;

namespace QuranBlazor
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>().UseMauiCommunityToolkit().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });
            builder.Services.AddMauiBlazorWebView();
            // Set path to the SQLite database (it will be created if it does not exist)
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, @"quran.db");
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<DialogService>();
            // Register DBContext and the SQLite database
            builder.Services.AddScoped<DbContext>(s => ActivatorUtilities.CreateInstance<DbContext>(s, dbPath));
            // Register ExportService
            builder.Services.AddScoped<ExportService>();
            return builder.Build();
        }
    }
}