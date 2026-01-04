using BarcodeScanning;
using CommunityToolkit.Maui;
using Syncfusion.Maui.Core.Hosting;

namespace FridgeScan;

public static partial class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureSyncfusionCore()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialSymbolsRounded.ttf", "Material");
                fonts.AddFont("Roboto-Medium.ttf", "Roboto-Medium");
                fonts.AddFont("Roboto-Regular.ttf", "Roboto-Regular");
            })
         .UseBarcodeScanning();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        // view models and services
        builder.Services.AddSingleton<ProductsViewModel>();
        builder.Services.AddSingleton<ImportViewModel>();
        builder.Services.AddSingleton<ActivitiesViewModel>();
        builder.Services.AddSingleton<RecipeViewModel>();

        builder.Services.AddSingleton<EmailService>();

        builder.Services.AddSingleton<ProductService>();
        builder.Services.AddSingleton<ActivityService>();
        builder.Services.AddSingleton<ProductsManager>();

        // pages
        builder.Services.AddTransient<Views.ProductsPage>();
        builder.Services.AddTransient<Views.ImportPage>();
        builder.Services.AddTransient<Views.RecipePage>();

#if ANDROID || IOS || MACCATALYST
        // Initialize Syncfusion license (replace with your license key)
        try
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Secrets.SyncfusionLicenseKey);
        }
        catch
        {
            // ignore if license call fails in design time
        }

#endif

        return builder.Build();
    }
}