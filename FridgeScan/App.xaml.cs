namespace FridgeScan;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		Application.Current!.UserAppTheme = AppTheme.Dark;

        // Sottoscrizione alle eccezioni del dominio corrente
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            Exception ex = (Exception)args.ExceptionObject;
            LogCrash(ex, "AppDomain.UnhandledException");
        };

        // Sottoscrizione alle eccezioni dei Task asincroni (molto comuni)
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            LogCrash(args.Exception, "TaskScheduler.UnobservedTaskException");
            args.SetObserved(); // Impedisce il crash immediato se possibile
        };

    }

    private void LogCrash(Exception ex, string source)
    {
        // Qui puoi fare tre cose:
        // 1. Stampare su console (visibile con adb logcat)
        Console.WriteLine($"CRITICAL_ERROR from {source}: {ex.Message}");
        Console.WriteLine($"STACKTRACE: {ex.StackTrace}");

        // 2. Salvare su un file locale per leggerlo dopo
        string logPath = Path.Combine(FileSystem.CacheDirectory, "crash_log.txt");
        File.WriteAllText(logPath, $"{DateTime.Now}: {source} - {ex}");

        // 3. (Opzionale) Inviare l'errore a un servizio come AppCenter o Sentry
    }

    protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
