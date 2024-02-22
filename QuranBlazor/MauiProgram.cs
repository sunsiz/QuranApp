using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using QuranBlazor.Data;

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
            //builder.Services.AddDbContextFactory<ContactContext>(opt =>
            //    opt.UseSqlite($"Data Source={nameof(ContactContext.ContactsDb)}.db"));
            builder.Services.AddSingleton<WeatherForecastService>();
            builder.Services.AddSingleton<DialogService>();
            // Register DBContext and the SQLite database
            builder.Services.AddScoped<DBContext>(s => ActivatorUtilities.CreateInstance<DBContext>(s, dbPath));
            return builder.Build();
        }
    }
}