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
    /// <summary>
    /// Holds a pending share URL until Shell is ready for navigation.
    /// Set during HandleShareIntent, consumed in ProcessPendingShareUrl.
    /// </summary>
    private static string? _pendingShareUrl;

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // Ensure MAUI platform is initialized so Platform.CurrentActivity is available for MSAL
        Microsoft.Maui.ApplicationModel.Platform.Init(this, savedInstanceState);

        // Handle cold-start share intent
        HandleShareIntent(Intent);
    }

    protected override void OnResume()
    {
        base.OnResume();
        // Shell is guaranteed to be initialised by the time OnResume fires.
        ProcessPendingShareUrl();
    }

    private void HandleShareIntent(Android.Content.Intent intent)
    {
        if (intent?.Action == Android.Content.Intent.ActionSend && intent?.Type == "text/plain")
        {
            var sharedText = intent.GetStringExtra(Android.Content.Intent.ExtraText);
            if (!string.IsNullOrWhiteSpace(sharedText))
            {
                _pendingShareUrl = Uri.EscapeDataString(sharedText);
                // Try now in case Shell is already available (warm start via OnNewIntent)
                ProcessPendingShareUrl();
            }
        }
    }

    private void ProcessPendingShareUrl()
    {
        if (_pendingShareUrl == null) return;
        if (Shell.Current == null) return; // still too early — OnResume will retry

        var url = _pendingShareUrl;
        _pendingShareUrl = null;          // consume the URL

        MainThread.BeginInvokeOnMainThread(() =>
            _ = NavigateToSharedRecipeAsync(url));
    }

    private static async Task NavigateToSharedRecipeAsync(string escapedUrl)
    {
        const string Tag = "FridgeScan.MainActivity";
        try
        {
            await Shell.Current.GoToAsync($"SharedRecipePage?url={escapedUrl}");
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"ShareIntent navigation failed: {ex}");
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
