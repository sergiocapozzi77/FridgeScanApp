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
[IntentFilter(new[] { Android.Content.Intent.ActionSend },
    Categories = new[] { Android.Content.Intent.CategoryDefault },
    DataMimeType = "text/plain")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // Ensure MAUI platform is initialized so Platform.CurrentActivity is available for MSAL
        Microsoft.Maui.ApplicationModel.Platform.Init(this, savedInstanceState);

        // Handle cold-start share intent
        HandleShareIntent(Intent);
    }

    private void HandleShareIntent(Android.Content.Intent intent)
    {
        if (intent?.Action == Android.Content.Intent.ActionSend && intent?.Type == "text/plain")
        {
            var sharedText = intent.GetStringExtra(Android.Content.Intent.ExtraText);
            if (!string.IsNullOrWhiteSpace(sharedText))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.GoToAsync($"SharedRecipe?url={Uri.EscapeDataString(sharedText)}");
                });
            }
        }
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

        if (intent?.Action == Android.Content.Intent.ActionSend && intent?.Type == "text/plain")
        {
            HandleShareIntent(intent);
        }
        else
        {
            // Existing MSAL auth redirect handling
            AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(0, Result.Ok, intent);
        }
    }
}
