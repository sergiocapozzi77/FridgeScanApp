using System.Diagnostics;

namespace FridgeScan.Services.RecipeImport;

/// <summary>
/// Fetches recipe page HTML using a platform WebView (real browser).
/// This bypasses bot protection (Cloudflare, etc.) that blocks HttpClient.
///
/// Platform support:
///   Android  — Android.Webkit.WebView with EvaluateJavascript
///   iOS/Mac  — WebKit.WKWebView with EvaluateJavaScript
///   Other    — returns null (not supported)
/// </summary>
public class WebViewHtmlFetcher : IRecipeHtmlFetcher, IDisposable
{
    private readonly TimeSpan _timeout;
    private bool _disposed;

    public WebViewHtmlFetcher() : this(TimeSpan.FromSeconds(20)) { }

    public WebViewHtmlFetcher(TimeSpan timeout)
    {
        _timeout = timeout;
    }

    public async Task<string?> FetchHtmlAsync(string url, CancellationToken ct = default)
    {
        Debug.WriteLine($"[WebViewHtmlFetcher] Fetching {url} via platform WebView");

#if ANDROID
        return await FetchOnAndroidAsync(url, ct);
#elif IOS || MACCATALYST
        return await FetchOnIosAsync(url, ct);
#else
        Debug.WriteLine("[WebViewHtmlFetcher] not supported on this platform");
        return await Task.FromResult<string?>(null);
#endif
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }

    // ---- Android (WebKit) ------------------------------------------------

#if ANDROID
    private async Task<string?> FetchOnAndroidAsync(string url, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<string?>();
        using var _ = ct.Register(() => tcs.TrySetCanceled());

        var context = Android.App.Application.Context;
        var webView = new Android.Webkit.WebView(context);

        try
        {
            webView.Settings.JavaScriptEnabled = true;
            webView.Settings.DomStorageEnabled = true;
            webView.Settings.CacheMode = Android.Webkit.CacheModes.NoCache;

            webView.SetWebViewClient(new AndroidPageClient(tcs));

            await MainThread.InvokeOnMainThreadAsync(() => webView.LoadUrl(url));

            var result = await tcs.Task.WaitAsync(_timeout);
            Debug.WriteLine(result is not null
                ? $"[WebViewHtmlFetcher] Android WebView returned {result.Length} chars"
                : "[WebViewHtmlFetcher] Android WebView returned null");
            return result;
        }
        finally
        {
            webView.Dispose();
        }
    }

    /// <summary>
    /// Custom WebViewClient that waits for the page to finish loading,
    /// then extracts the outerHTML via JavaScript evaluation.
    /// </summary>
    private sealed class AndroidPageClient : Android.Webkit.WebViewClient
    {
        private readonly TaskCompletionSource<string?> _tcs;

        public AndroidPageClient(TaskCompletionSource<string?> tcs) => _tcs = tcs;

        public override void OnPageFinished(Android.Webkit.WebView view, string url)
        {
            base.OnPageFinished(view, url);

            // Run JS on UI thread — EvaluateJavascript requires it
            MainThread.BeginInvokeOnMainThread(() =>
            {
                view.EvaluateJavascript(
                    "(function() { return document.documentElement.outerHTML; })();",
                    new JsResultCallback(result => _tcs.TrySetResult(DecodeJsResult(result))));
            });
        }

        public override void OnReceivedHttpError(
            Android.Webkit.WebView view,
            Android.Webkit.IWebResourceRequest? request,
            Android.Webkit.WebResourceResponse? errorResponse)
        {
            base.OnReceivedHttpError(view, request, errorResponse);
            if (request?.IsForMainFrame == true && errorResponse is not null)
                _tcs.TrySetResult(null);
        }

        public override void OnReceivedError(
            Android.Webkit.WebView view,
            Android.Webkit.IWebResourceRequest? request,
            Android.Webkit.WebResourceError? error)
        {
            base.OnReceivedError(view, request, error);
            if (request?.IsForMainFrame == true)
                _tcs.TrySetResult(null);
        }
    }

    /// <summary>
    /// Android IValueCallback adaptor — receives the JavaScript evaluation result.
    /// The result is a JSON-encoded string (always wrapped in quotes with escaping).
    /// </summary>
    private sealed class JsResultCallback : Java.Lang.Object, Android.Webkit.IValueCallback
    {
        private readonly Action<string?> _callback;
        public JsResultCallback(Action<string?> callback) => _callback = callback;
        public void OnReceiveValue(Java.Lang.Object? value) => _callback(value?.ToString());
    }

    /// <summary>
    /// Android EvaluateJavascript returns the result as a JSON-encoded string.
    /// We need to deserialize it to get the actual HTML.
    /// </summary>
    private static string? DecodeJsResult(string? jsonEncoded)
    {
        if (string.IsNullOrEmpty(jsonEncoded) || jsonEncoded == "\"\"")
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<string>(jsonEncoded);
        }
        catch
        {
            // Fallback: strip surrounding quotes and unescape common sequences
            if (jsonEncoded.Length >= 2 && jsonEncoded[0] == '"' && jsonEncoded[^1] == '"')
                return jsonEncoded[1..^1].Replace("\\\"", "\"").Replace("\\n", "\n");
            return jsonEncoded;
        }
    }
#endif

    // ---- iOS / MacCatalyst (WebKit) --------------------------------------

#if IOS || MACCATALYST
    private async Task<string?> FetchOnIosAsync(string url, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<string?>();
        using var _ = ct.Register(() => tcs.TrySetCanceled());

        var webView = new WebKit.WKWebView(
            CoreGraphics.CGRect.Empty,
            new WebKit.WKWebViewConfiguration());

        try
        {
            webView.NavigationDelegate = new IosNavigationDelegate(tcs);

            await MainThread.InvokeOnMainThreadAsync(() =>
                webView.LoadRequest(new Foundation.NSUrlRequest(new Foundation.NSUrl(url))));

            var result = await tcs.Task.WaitAsync(_timeout);
            Debug.WriteLine(result is not null
                ? $"[WebViewHtmlFetcher] iOS WebView returned {result.Length} chars"
                : "[WebViewHtmlFetcher] iOS WebView returned null");
            return result;
        }
        finally
        {
            webView.Dispose();
        }
    }

    /// <summary>
    /// WKNavigationDelegate that waits for the page to finish loading,
    /// then extracts the outerHTML via JavaScript evaluation.
    /// </summary>
    private sealed class IosNavigationDelegate : WebKit.WKNavigationDelegate
    {
        private readonly TaskCompletionSource<string?> _tcs;

        public IosNavigationDelegate(TaskCompletionSource<string?> tcs) => _tcs = tcs;

        public override void DidFinishNavigation(
            WebKit.WKWebView webView,
            WebKit.WKNavigation navigation)
        {
            webView.EvaluateJavaScript(
                "document.documentElement.outerHTML",
                (result, error) =>
                {
                    if (error is not null)
                    {
                        Debug.WriteLine($"[WebViewHtmlFetcher] iOS JS error: {error}");
                        _tcs.TrySetResult(null);
                    }
                    else
                    {
                        _tcs.TrySetResult(result?.ToString());
                    }
                });
        }

        public override void DidFailNavigation(
            WebKit.WKWebView webView,
            WebKit.WKNavigation navigation,
            Foundation.NSError error)
        {
            Debug.WriteLine($"[WebViewHtmlFetcher] iOS navigation failed: {error}");
            _tcs.TrySetResult(null);
        }

        public override void DidFailProvisionalNavigation(
            WebKit.WKWebView webView,
            Foundation.NSError error)
        {
            Debug.WriteLine($"[WebViewHtmlFetcher] iOS provisional nav failed: {error}");
            _tcs.TrySetResult(null);
        }
    }
#endif
}
