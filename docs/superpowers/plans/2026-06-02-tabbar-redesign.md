# A3 Floating Shelf TabBar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the default Shell TabBar with a custom floating bottom navigation bar ("Floating Shelf") following M3 dark theme design tokens.

**Architecture:** Shell still handles page routing and navigation. `Shell.TabBarIsVisible="False"` hides the native bar. A reusable `FloatingBottomBar` ContentView sits at the bottom of each page via a two-row Grid `(RowDefinitions="*,Auto")`. Tab switching calls `Shell.Current.GoToAsync("//route")`. Active tab detection listens to `Shell.Current.Navigated`.

**Tech Stack:** .NET MAUI, Shell navigation, CommunityToolkit.Mvvm

**Spec:** `docs/superpowers/specs/2026-06-02-tabbar-redesign-design.md`

---

### Task 1: Create FloatingBottomBar control

**Files:**
- Create: `FridgeScan/Controls/FloatingBottomBar.xaml`
- Create: `FridgeScan/Controls/FloatingBottomBar.xaml.cs`

- [ ] **Step 1: Create the XAML layout**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="FridgeScan.Controls.FloatingBottomBar"
             x:Name="FloatingBar">

    <!-- Floating shelf container -->
    <Border BackgroundColor="#14142E"
            StrokeShape="RoundRectangle 16"
            Stroke="Transparent"
            Padding="8,6"
            Margin="12,0,12,8"
            Shadow="{Binding Source={x:Reference FloatingBar}, Path=BarShadow}">
        <HorizontalStackLayout x:Name="TabsContainer"
                               HorizontalOptions="FillAndExpand"
                               Spacing="0">
            <!-- Tabs are built programmatically in code-behind -->
        </HorizontalStackLayout>
    </Border>
</ContentView>
```

- [ ] **Step 2: Create the code-behind**

```csharp
using System.Text.RegularExpressions;

namespace FridgeScan.Controls;

public partial class FloatingBottomBar : ContentView
{
    private readonly List<TabItem> _tabs = new();
    private string _activeRoute = "";

    public Shadow BarShadow => new Shadow
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
        Loaded += (s, e) =>
        {
            if (Shell.Current != null)
            {
                Shell.Current.Navigated += OnShellNavigated;
                UpdateActiveTab(Shell.Current.CurrentState.Location.ToString());
            }
        };
        Unloaded += (s, e) =>
        {
            if (Shell.Current != null)
                Shell.Current.Navigated -= OnShellNavigated;
        };
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
            if (_activeRoute != route)
                await Shell.Current.GoToAsync(route);
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
        var match = Regex.Match(location, @"^//(\w+)");
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
```

- [ ] **Step 3: Check it builds**

```bash
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | head -30
```

Expected: Build succeeds with a warning about unused FloatingBottomBar (fine — pages don't use it yet).

- [ ] **Step 4: Commit**

```bash
git add FridgeScan/Controls/FloatingBottomBar.xaml FridgeScan/Controls/FloatingBottomBar.xaml.cs
git commit -m "feat: add FloatingBottomBar control

- M3 dark theme floating shelf bottom navigation bar
- 5 tabs: Products, Recipe, Cookbooks, Import, Activity
- Auto-detects active tab from Shell.Current.Navigated
- Active tab: #1E1E3A pill background, white text
- Inactive: #8888AA text, transparent background
- Shadow elevation for floating effect

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Add explicit Routes and hide Shell TabBar

**Files:**
- Modify: `FridgeScan/AppShell.xaml` (all ShellContent elements + TabBar)

- [ ] **Step 1: Add Route attributes to ShellContent and hide TabBar**

Each `ShellContent` needs an explicit `Route` so the FloatingBottomBar can navigate to a predictable URI. Also hide the native TabBar visual.

Replace the `<TabBar>` block (lines 11-54) with:

```xml
    <TabBar Shell.TabBarIsVisible="False">
        <ShellContent Title="Products" Route="products" ContentTemplate="{DataTemplate views:ProductsPage}">
            <ShellContent.Icon>
                <FontImageSource FontFamily="Material" Glyph="&#xe85d;" />
            </ShellContent.Icon>
        </ShellContent>
        <ShellContent Title="Recipe" Route="recipe" ContentTemplate="{DataTemplate views:RecipePage}">
            <ShellContent.Icon>
                <FontImageSource FontFamily="Material" Glyph="&#xf357;" />
            </ShellContent.Icon>
        </ShellContent>
        <ShellContent Title="Import" Route="import" ContentTemplate="{DataTemplate views:ImportPage}"
                      Icon="icon_import.png" />
        <ShellContent Title="Activities" Route="activities" ContentTemplate="{DataTemplate views:ActivitiesPage}">
            <ShellContent.Icon>
                <FontImageSource FontFamily="Material" Glyph="&#xe889;" />
            </ShellContent.Icon>
        </ShellContent>
        <ShellContent Title="Cookbooks" Route="cookbook" ContentTemplate="{DataTemplate views:CookbookPage}">
            <ShellContent.Icon>
                <FontImageSource FontFamily="Material" Glyph="&#xe86d;" />
            </ShellContent.Icon>
        </ShellContent>
    </TabBar>
```

Changes: `Route="products"` (etc.) added to each ShellContent. `Shell.TabBarIsVisible="False"` hides the native bar.

- [ ] **Step 2: Build to verify**

```bash
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -10
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add FridgeScan/AppShell.xaml
git commit -m "feat: add explicit Shell routes and hide TabBar

- Add Route attributes to all 5 ShellContent items
- Shell.TabBarIsVisible=False hides native visual bar
- Routes: products, recipe, import, activities, cookbook
- Enables custom FloatingBottomBar navigation with //route

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Update ProductsPage to include FloatingBottomBar

**Files:**
- Modify: `FridgeScan/Views/ProductsPage.xaml`

- [ ] **Step 1: Add FloatingBottomBar namespace to ProductsPage.xaml**

At the top of the root `ContentPage` element, add this XML namespace:

```xml
xmlns:controls="clr-namespace:FridgeScan.Controls"
```

- [ ] **Step 2: Wrap root content in a Grid with FloatingBottomBar**

The current root is:
```xml
    <Grid Padding="10">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>
        ...
    </Grid>
```

Replace the outer Grid tag (`<Grid Padding="10">` and its closing `</Grid>`) with a wrapper Grid:

```xml
    <Grid RowDefinitions="*,Auto">
        <Grid Padding="10" Grid.Row="0">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
            </Grid.RowDefinitions>
            <!--  AUTOCOMPLETE + BARCODE  -->
            <Grid>
                <!-- existing content stays exactly as-is -->
            ...
            <!-- all existing content up to the closing </Grid> of the original root -->
        </Grid>

        <!-- Floating Bottom Bar -->
        <controls:FloatingBottomBar Grid.Row="1" />
    </Grid>
```

The exact change: wrap the existing `Grid Padding="10"` content in a new outer Grid with `RowDefinitions="*,Auto"`, move the existing content to `Grid.Row="0"`, and add `FloatingBottomBar` at `Grid.Row="1"`.

- [ ] **Step 3: Build to verify**

```bash
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -10
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add FridgeScan/Views/ProductsPage.xaml
git commit -m "feat: add FloatingBottomBar to ProductsPage

- Wrap page content in *,Auto Grid layout
- Add FloatingBottomBar in auto-sized bottom row

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Update RecipePage to include FloatingBottomBar

**Files:**
- Modify: `FridgeScan/Views/RecipePage.xaml`

- [ ] **Step 1: Add FloatingBottomBar namespace to RecipePage.xaml**

```xml
xmlns:controls="clr-namespace:FridgeScan.Controls"
```

- [ ] **Step 2: Wrap root content in a Grid with FloatingBottomBar**

The current root `<Grid>` contains 5 rows (Auto, Auto, Auto, Auto, *). Wrap it in a new outer Grid:

```xml
    <Grid RowDefinitions="*,Auto">
        <Grid Grid.Row="0">
            <!-- existing Grid with its 5 rows stays exactly as-is -->
            ...
        </Grid>

        <!-- Floating Bottom Bar -->
        <controls:FloatingBottomBar Grid.Row="1" />
    </Grid>
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -10
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add FridgeScan/Views/RecipePage.xaml
git commit -m "feat: add FloatingBottomBar to RecipePage

- Wrap page content in *,Auto Grid layout
- Add FloatingBottomBar in bottom row

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: Update ImportPage to include FloatingBottomBar

**Files:**
- Modify: `FridgeScan/Views/ImportPage.xaml`

- [ ] **Step 1: Add FloatingBottomBar namespace**

```xml
xmlns:controls="clr-namespace:FridgeScan.Controls"
```

- [ ] **Step 2: Wrap root content in a Grid with FloatingBottomBar**

Current root is `<VerticalStackLayout Padding="20" Spacing="20">`. Wrap it:

```xml
    <Grid RowDefinitions="*,Auto">
        <VerticalStackLayout Grid.Row="0" Padding="20" Spacing="20">
            <!-- existing content stays exactly as-is -->
        </VerticalStackLayout>

        <!-- Floating Bottom Bar -->
        <controls:FloatingBottomBar Grid.Row="1" />
    </Grid>
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -10
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add FridgeScan/Views/ImportPage.xaml
git commit -m "feat: add FloatingBottomBar to ImportPage

- Wrap page content in *,Auto Grid layout
- Add FloatingBottomBar in bottom row

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: Update ActivitiesPage to include FloatingBottomBar

**Files:**
- Modify: `FridgeScan/Views/ActivitiesPage.xaml`

- [ ] **Step 1: Add FloatingBottomBar namespace**

```xml
xmlns:controls="clr-namespace:FridgeScan.Controls"
```

- [ ] **Step 2: Wrap root content in a Grid with FloatingBottomBar**

Current root `<Grid>` contains `SfPullToRefresh`. Wrap it:

```xml
    <Grid RowDefinitions="*,Auto">
        <Grid Grid.Row="0">
            <!-- existing content (SfPullToRefresh with its inner Grid) stays exactly as-is -->
            ...
        </Grid>

        <!-- Floating Bottom Bar -->
        <controls:FloatingBottomBar Grid.Row="1" />
    </Grid>
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -10
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add FridgeScan/Views/ActivitiesPage.xaml
git commit -m "feat: add FloatingBottomBar to ActivitiesPage

- Wrap page content in *,Auto Grid layout
- Add FloatingBottomBar in bottom row

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 7: Update CookbookPage to include FloatingBottomBar

**Files:**
- Modify: `FridgeScan/Views/CookbookPage.xaml`

- [ ] **Step 1: Add FloatingBottomBar namespace**

```xml
xmlns:controls="clr-namespace:FridgeScan.Controls"
```

- [ ] **Step 2: Wrap root content in a Grid with FloatingBottomBar**

Current root `<Grid RowDefinitions="Auto,*">`. Wrap it:

```xml
    <Grid RowDefinitions="*,Auto">
        <Grid Grid.Row="0" RowDefinitions="Auto,*">
            <!-- existing header Grid (row 0) and SfPullToRefresh (row 1) stay exactly as-is -->
            ...
        </Grid>

        <!-- Floating Bottom Bar -->
        <controls:FloatingBottomBar Grid.Row="1" />
    </Grid>
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -10
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add FridgeScan/Views/CookbookPage.xaml
git commit -m "feat: add FloatingBottomBar to CookbookPage

- Wrap page content in *,Auto Grid layout
- Add FloatingBottomBar in bottom row

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 8: Final build verification

**Files:** none — just build the whole solution.

- [ ] **Step 1: Build all targets**

```bash
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -10
```

Expected: Build succeeded with 0 errors, 0 warnings.

- [ ] **Step 2: Verify the diff is complete**

```bash
git diff --stat
```

Expected: Shows changes to AppShell.xaml + 5 page XAML files + 2 new FloatingBottomBar files.

- [ ] **Step 3: Make a final commit for any lingering changes**

```bash
git add -A
git commit -m "feat: complete A3 Floating Shelf bottom navigation bar

- Custom FloatingBottomBar replaces Shell TabBar
- M3 dark theme colors throughout
- 5 tabs with route-based active detection
- Applied to all 5 tab pages

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```
