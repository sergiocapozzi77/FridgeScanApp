using System.Diagnostics;

#if ANDROID
using Android.Util;
#endif

namespace FridgeScan.Helpers;

/// <summary>
/// Centralized logger that writes to Android Logcat on Android
/// and System.Diagnostics.Debug on all other platforms.
/// </summary>
public static class Logger
{
    /// <summary>
    /// Writes a debug-level log message.
    /// On Android this goes to Log.Debug (visible in logcat with filter "tag").
    /// On other platforms this goes to Debug.WriteLine.
    /// </summary>
    [Conditional("DEBUG")]
    public static void Debug(string tag, string message)
    {
#if ANDROID
        Log.Debug(tag, message);
#else
        System.Diagnostics.Debug.WriteLine($"[{tag}] {message}");
#endif
    }

    /// <summary>
    /// Writes an error-level log message, optionally with exception details.
    /// On Android this goes to Log.Error (visible in logcat with filter "tag").
    /// On other platforms this goes to Debug.WriteLine.
    /// </summary>
    [Conditional("DEBUG")]
    public static void Error(string tag, string message, Exception? ex = null)
    {
#if ANDROID
        Log.Error(tag, ex != null ? $"{message}: {ex}" : message);
#else
        System.Diagnostics.Debug.WriteLine($"[{tag}][ERROR] {message}{(ex != null ? $": {ex}" : "")}");
#endif
    }
}
