using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Microsoft.Identity.Client;

namespace FridgeScan;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[] { Android.Content.Intent.ActionView },
    Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
    DataSchemes = new[] { "msalf6c0b2ba-e930-44c0-97e3-00ca28a3cdf3" },
    DataHosts = new[] { "auth" })]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // Ensure MAUI platform is initialized so Platform.CurrentActivity is available for MSAL
        Microsoft.Maui.ApplicationModel.Platform.Init(this, savedInstanceState);
    }

    protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Android.Content.Intent data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        // Return control to MSAL
      //  AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
    }

    protected override void OnNewIntent(Android.Content.Intent intent)
    {
        base.OnNewIntent(intent);
        try
        {
            // Let MAUI handle the intent and log the incoming data for debugging
            Microsoft.Maui.ApplicationModel.Platform.OnNewIntent(intent);
            var data = intent?.DataString ?? "<no-data>";
            Android.Util.Log.Debug("FridgeScan", "OnNewIntent received: " + data);
            AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(0, Result.Ok, intent);
            // Notify the shared app that a redirect was received so awaiting flows can resume
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("FridgeScan", "OnNewIntent error: " + ex);
        }
    }
}
