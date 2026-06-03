using System.Text.RegularExpressions;

namespace FridgeScan.Controls;

public partial class FloatingBottomBar : ContentView
{
    private const string Tag = "FridgeScan.FloatingBottomBar";

    private readonly List<TabItem> _tabs = new();
    private string _activeRoute = "";
    private bool _subscribed;

    private static readonly Regex RouteRegex = new(@"^//(\w+)", RegexOptions.Compiled);

    public Shadow BarShadow { get; } = new Shadow
    {
        Brush = new SolidColorBrush(Colors.Black),
        Opacity = 0.3f,
        Offset = new Point(0, 4),
        Radius = 16f
    };

    public FloatingBottomBar()
    {
        InitializeComponent();
        BuildTabs();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, EventArgs e)
    {
        if (!_subscribed && Shell.Current != null)
        {
            Shell.Current.Navigated += OnShellNavigated;
            _subscribed = true;
        }
        UpdateActiveTab(Shell.Current?.CurrentState?.Location?.ToString() ?? "");
    }

    private void OnUnloaded(object sender, EventArgs e)
    {
        if (_subscribed && Shell.Current != null)
        {
            Shell.Current.Navigated -= OnShellNavigated;
            _subscribed = false;
        }
    }

    private void BuildTabs()
    {
        // Glyph codepoints from Material Icons font:
        // Products =  (inventory), Recipe =  (cookbook),
        // Import =  (file download), Activity =  (notifications),
        // Cookbooks =  (book)
        var tabs = new (string glyph, string label, string route)[]
        {
            ("", "Products",  "//products"),
            ("", "Recipe",    "//recipe"),
            ("", "Import",    "//import"),
            ("", "Activity",  "//activities"),
            ("", "Cookbooks", "//cookbook"),
        };

        foreach (var (glyph, label, route) in tabs)
        {
            var tab = CreateTab(glyph, label, route);
            _tabs.Add(new TabItem { Border = tab.border, GlyphLabel = tab.glyphLabel, TextLabel = tab.textLabel, Route = route });
            TabsContainer.Children.Add(tab.border);
        }
    }

    private (Border border, Label glyphLabel, Label textLabel) CreateTab(string glyph, string label, string route)
    {
        var glyphLabel = new Label
        {
            Text = glyph,
            FontFamily = "Material",
            FontSize = 20,
            TextColor = Color.FromArgb("#8888AA"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HeightRequest = 24,
        };

        var textLabel = new Label
        {
            Text = label,
            FontSize = 9,
            TextColor = Color.FromArgb("#8888AA"),
            HorizontalOptions = LayoutOptions.Center,
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 1,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Children = { glyphLabel, textLabel }
        };

        var border = new Border
        {
            BackgroundColor = Colors.Transparent,
            Stroke = Colors.Transparent,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            WidthRequest = 64,
            HeightRequest = 48,
            Padding = new Thickness(0),
            Content = stack,
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            if (Shell.Current == null || string.Equals(_activeRoute, route, StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                await Shell.Current.GoToAsync(route);
            }
            catch (Exception ex)
            {
                Logger.Error(Tag, $"FloatingBottomBar navigation failed: {ex.Message}");
            }
        };
        border.GestureRecognizers.Add(tapGesture);

        return (border, glyphLabel, textLabel);
    }

    private void OnShellNavigated(object sender, ShellNavigatedEventArgs e)
    {
        UpdateActiveTab(e.Current?.Location?.ToString() ?? "");
    }

    private void UpdateActiveTab(string location)
    {
        // Extract the tab route from the Shell URI (e.g., "//products/detail" -> "products")
        var match = RouteRegex.Match(location);
        var currentTab = match.Success ? match.Groups[1].Value.ToLower() : "";
        _activeRoute = $"//{currentTab}";

        foreach (var tab in _tabs)
        {
            var isActive = string.Equals(tab.Route, $"//{currentTab}", StringComparison.OrdinalIgnoreCase);
            tab.Border.BackgroundColor = isActive ? Color.FromArgb("#1E1E3A") : Colors.Transparent;
            tab.GlyphLabel.TextColor = isActive ? Colors.White : Color.FromArgb("#8888AA");
            tab.TextLabel.TextColor = isActive ? Colors.White : Color.FromArgb("#8888AA");
        }
    }

    private class TabItem
    {
        public Border Border { get; set; }
        public Label GlyphLabel { get; set; }
        public Label TextLabel { get; set; }
        public string Route { get; set; }
    }
}
