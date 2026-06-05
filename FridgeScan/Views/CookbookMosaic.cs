using FridgeScan.Controls;

namespace FridgeScan.Views;

public class CookbookMosaic : ContentView
{
    public static readonly BindableProperty ImageUrlsProperty =
        BindableProperty.Create(nameof(ImageUrls), typeof(IList<string>), typeof(CookbookMosaic),
            propertyChanged: OnImageUrlsChanged);

    public IList<string>? ImageUrls
    {
        get => (IList<string>?)GetValue(ImageUrlsProperty);
        set => SetValue(ImageUrlsProperty, value);
    }

    private readonly Grid _grid = new();

    public CookbookMosaic()
    {
        _grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        _grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        _grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        _grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        Content = _grid;
        RenderMosaic();
    }

    private static void OnImageUrlsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((CookbookMosaic)bindable).RenderMosaic();
    }

    private void RenderMosaic()
    {
        _grid.Children.Clear();
        var urls = ImageUrls;
        int count = urls?.Count ?? 0;

        switch (count)
        {
            case 0:
                _grid.Children.Add(new Label
                {
                    Text = "\U0001f372",
                    FontSize = 28,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Colors.Gray
                });
                Grid.SetRowSpan((View)_grid.Children[0], 2);
                Grid.SetColumnSpan((View)_grid.Children[0], 2);
                break;

            case 1:
                _grid.Children.Add(CreateImage(urls![0]));
                Grid.SetRowSpan((View)_grid.Children[0], 2);
                Grid.SetColumnSpan((View)_grid.Children[0], 2);
                break;

            case 2:
                _grid.Children.Add(CreateImage(urls![0]));
                Grid.SetRowSpan((View)_grid.Children[0], 2);
                _grid.Children.Add(CreateImage(urls![1]));
                Grid.SetColumn((View)_grid.Children[1], 1);
                Grid.SetRowSpan((View)_grid.Children[1], 2);
                break;

            case 3:
                _grid.Children.Add(CreateImage(urls![0]));
                Grid.SetRowSpan((View)_grid.Children[0], 2);
                _grid.Children.Add(CreateImage(urls![1]));
                Grid.SetColumn((View)_grid.Children[1], 1);
                _grid.Children.Add(CreateImage(urls![2]));
                Grid.SetColumn((View)_grid.Children[2], 1);
                Grid.SetRow((View)_grid.Children[2], 1);
                break;

            default: // 4+
                _grid.Children.Add(CreateImage(urls![0]));
                _grid.Children.Add(CreateImage(urls![1]));
                Grid.SetColumn((View)_grid.Children[1], 1);
                _grid.Children.Add(CreateImage(urls![2]));
                Grid.SetRow((View)_grid.Children[2], 1);
                _grid.Children.Add(CreateImage(urls![3]));
                Grid.SetRow((View)_grid.Children[3], 1);
                Grid.SetColumn((View)_grid.Children[3], 1);
                break;
        }
    }

    private static ProgressiveImage CreateImage(string url)
    {
        return new ProgressiveImage
        {
            Source = url,
            Aspect = Aspect.AspectFill,
            CornerRadius = 0,
            PlaceholderColor = Color.FromArgb("#161638"),
        };
    }
}
