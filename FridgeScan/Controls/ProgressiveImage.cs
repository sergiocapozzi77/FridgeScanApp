using System.Collections.Concurrent;
using System.Net.Http;
using Microsoft.Maui.Controls.Shapes;

namespace FridgeScan.Controls;

/// <summary>
/// An image control that shows a solid placeholder immediately while downloading
/// the remote image via HttpClient, then crossfades to the real image.
/// Downloaded bytes are cached in-memory so back-navigation loads instantly.
/// </summary>
public class ProgressiveImage : ContentView
{
    #region Bindable Properties

    public static readonly BindableProperty SourceProperty =
        BindableProperty.Create(nameof(Source), typeof(string), typeof(ProgressiveImage),
            defaultValue: string.Empty, propertyChanged: OnSourceChanged);

    public static readonly BindableProperty CornerRadiusProperty =
        BindableProperty.Create(nameof(CornerRadius), typeof(double), typeof(ProgressiveImage),
            defaultValue: 0.0, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty AspectProperty =
        BindableProperty.Create(nameof(Aspect), typeof(Aspect), typeof(ProgressiveImage),
            defaultValue: Microsoft.Maui.Aspect.AspectFill);

    public static readonly BindableProperty PlaceholderColorProperty =
        BindableProperty.Create(nameof(PlaceholderColor), typeof(Color), typeof(ProgressiveImage),
            defaultValue: Color.FromArgb("#161638"), propertyChanged: OnVisualPropertyChanged);

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public Aspect Aspect
    {
        get => (Aspect)GetValue(AspectProperty);
        set => SetValue(AspectProperty, value);
    }

    public Color PlaceholderColor
    {
        get => (Color)GetValue(PlaceholderColorProperty);
        set => SetValue(PlaceholderColorProperty, value);
    }

    #endregion

    #region Internal state

    private readonly Border _placeholder;
    private readonly Image _image;
    private readonly Label _errorLabel;

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static readonly ConcurrentDictionary<string, byte[]> _cache = new();

    private CancellationTokenSource? _cts;

    #endregion

    public ProgressiveImage()
    {
        _image = new Image
        {
            Opacity = 0,
            Aspect = Aspect.AspectFill,
        };

        _placeholder = new Border
        {
            BackgroundColor = PlaceholderColor,
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(CornerRadius) },
        };

        _errorLabel = new Label
        {
            Text = "\U0001f37d️", // 🍽️ fork and knife emoji
            FontSize = 24,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            IsVisible = false,
        };

        var grid = new Grid();
        grid.Children.Add(_placeholder);
        grid.Children.Add(_image);
        grid.Children.Add(_errorLabel);

        Content = grid;
    }

    #region Property changed handlers

    private static async void OnSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (ProgressiveImage)bindable;
        await control.LoadImageAsync();
    }

    private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (ProgressiveImage)bindable;
        control._placeholder.BackgroundColor = control.PlaceholderColor;
        control._placeholder.StrokeShape = new RoundRectangle
        {
            CornerRadius = new CornerRadius(control.CornerRadius)
        };
    }

    #endregion

    #region Image loading

    private async Task LoadImageAsync()
    {
        var url = Source;

        // Cancel any in-flight download from a previous binding change
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        // Reset to loading state
        _image.Opacity = 0;
        _image.Source = null;
        _errorLabel.IsVisible = false;
        _placeholder.IsVisible = true;
        _placeholder.Opacity = 1;
        _placeholder.BackgroundColor = PlaceholderColor;
        _placeholder.StrokeShape = new RoundRectangle
        {
            CornerRadius = new CornerRadius(CornerRadius)
        };
        _image.Aspect = Aspect;

        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            // Check / populate in-memory cache
            if (!_cache.TryGetValue(url, out var bytes))
            {
                bytes = await _httpClient.GetByteArrayAsync(url, token);
                if (bytes.Length == 0)
                    throw new InvalidOperationException("Empty image data");

                _cache[url] = bytes;
            }

            token.ThrowIfCancellationRequested();

            // Hand the bytes to MAUI via a stream provider
            var capturedBytes = bytes;
            _image.Source = ImageSource.FromStream(() => new MemoryStream(capturedBytes));

            // Wait one frame so the Image element can start rendering
            await Task.Delay(16, token);

            token.ThrowIfCancellationRequested();

            // Crossfade: image fades in, placeholder fades out
            await Task.WhenAll(
                _image.FadeTo(1, 300, Easing.CubicOut),
                _placeholder.FadeTo(0, 200, Easing.CubicOut)
            );

            _placeholder.IsVisible = false;
            _placeholder.Opacity = 1; // reset for potential reuse
        }
        catch (OperationCanceledException)
        {
            // A new Source was set before this one finished — the next load handles it
        }
        catch (Exception)
        {
            if (!token.IsCancellationRequested)
            {
                _placeholder.IsVisible = true;
                _placeholder.Opacity = 1;
                _errorLabel.IsVisible = true;
            }
        }
    }

    #endregion
}
