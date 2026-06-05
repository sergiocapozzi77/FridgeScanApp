using Microsoft.Maui.Controls.Shapes;

namespace FridgeScan.Controls;

/// <summary>
/// An image control that shows a solid placeholder immediately while
/// MAUI/Glide loads the remote image natively. After a brief minimum
/// display time the image crossfades in, avoiding the blank-rectangle
/// pop that normally happens with direct Image bindings.
///
/// The real image is loaded via ImageSource.FromUri so that platform
/// optimisations (Glide disk cache on Android, efficient decoding)
/// are preserved — unlike a raw HttpClient download.
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

    public static readonly BindableProperty TransitionDelayProperty =
        BindableProperty.Create(nameof(TransitionDelay), typeof(int), typeof(ProgressiveImage),
            defaultValue: 400);

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

    /// <summary>Minimum ms the placeholder is visible before crossfade starts.</summary>
    public int TransitionDelay
    {
        get => (int)GetValue(TransitionDelayProperty);
        set => SetValue(TransitionDelayProperty, value);
    }

    #endregion

    #region Internal state

    private readonly Border _placeholder;
    private readonly Image _image;
    private readonly Label _errorLabel;
    private CancellationTokenSource? _cts;

    /// <summary>Crossfade duration for the image fade-in.</summary>
    private const uint FadeDuration = 400;

    /// <summary>Crossfade duration for the placeholder fade-out.</summary>
    private const uint PlaceholderFadeDuration = 300;

    #endregion

    public ProgressiveImage()
    {
        _image = new Image
        {
            Opacity = 0,
            Aspect = Microsoft.Maui.Aspect.AspectFill,
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
            Text = "\U0001f37d️",
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

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        // Reset to loading state (placeholder visible, image hidden)
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
            // Start native image loading (Glide on Android, URLSession on iOS, etc.)
            // This uses the platform's built-in disk cache and efficient decoding.
            _image.Source = ImageSource.FromUri(new Uri(url));

            // Wait the minimum display time so the placeholder is
            // perceptible even when the image is cached.
            await Task.Delay(TransitionDelay, token);

            token.ThrowIfCancellationRequested();

            // Crossfade: image fades in, placeholder fades out
            await Task.WhenAll(
                _image.FadeTo(1, FadeDuration, Easing.CubicOut),
                _placeholder.FadeTo(0, PlaceholderFadeDuration, Easing.CubicOut)
            );

            _placeholder.IsVisible = false;
            _placeholder.Opacity = 1; // reset for reuse
        }
        catch (OperationCanceledException)
        {
            // A new Source was set — the next load handles it
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
