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

    /// <summary>
    /// Handler for share intents from other apps.
    /// Android share sheets often include text before the URL
    /// (e.g. "Recipe Title | SiteName https://share.google/...").
    /// Extracts only the URL so the import pipeline receives a clean address.
    /// </summary>
    private void HandleShareIntent(Android.Content.Intent intent)
    {
        if (intent?.Action == Android.Content.Intent.ActionSend && intent?.Type == "text/plain")
        {
            var sharedText = intent.GetStringExtra(Android.Content.Intent.ExtraText);
            if (!string.IsNullOrWhiteSpace(sharedText))
            {
                var url = ExtractUrlFromText(sharedText);
                Logger.Debug("FridgeScan.ShareIntent",
                    $"sharedText='{sharedText}' extractedUrl='{url ?? "(null)"}'");

                if (url != null)
                {
                    _pendingShareUrl = Uri.EscapeDataString(url);
                    // Try now in case Shell is already available (warm start via OnNewIntent)
                    ProcessPendingShareUrl();
                }
            }
        }
    }

    /// <summary>
    /// Extracts the last absolute http/https URL from a block of shared text.
    /// Android share sheets commonly attach a title before the URL,
    /// e.g. "Ciasto rabarbarowe | AniaGotuje.pl https://share.google/6dapkM9DoDmfTCRqz"
    /// — this method picks out the URL portion.
    /// Falls back to the full text if no http/https URL is found
    /// (preserves existing behaviour for plain-URL shares).
    /// </summary>
    private static string? ExtractUrlFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        // Walk backwards through whitespace-delimited tokens and return
        // the last one that looks like an absolute http/https URL.
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            if (Uri.TryCreate(parts[i], UriKind.Absolute, out var uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                return parts[i];
            }
        }

        // No URL-like token found — let the existing pipeline handle it as-is.
        return text;
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
