# Products Page Toolbar + Floating Barcode FAB Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a compact action toolbar (Search/Filter/Sort) between the page header and Add Item card, plus a floating 56dp barcode FAB in the bottom-right of the product list.

**Architecture:** Three files change: `ProductsViewModel.cs` gains filter/sort/search state and a `RefreshDisplay()` method that replaces `RefreshGrouping()`. `ProductsPage.xaml` is restructured (header loses action buttons, gains toolbar pills + segment panels + FAB). `ProductsPage.xaml.cs` gets chip tap handlers and a search expand animation.

**Tech Stack:** .NET MAUI, CommunityToolkit.Mvvm, Syncfusion ListView

---

### Task 1: Add filter/sort enums and observable properties to ProductsViewModel

**Files:**
- Modify: `FridgeScan/ViewModels/ProductsViewModel.cs` (top of file, after existing fields)

- [ ] **Step 1: Add the two enums above the class**

```csharp
// Add before public partial class ProductsViewModel
namespace FridgeScan.ViewModels;

public enum ProductFilterMode { None, ExpiringSoon, Expired }
public enum ProductSortMode { Alphabetical, ByExpiry }

public partial class ProductsViewModel : BaseViewModel
{
```

- [ ] **Step 2: Add observable properties inside the class, after `BarcodeCommand`**

```csharp
public ICommand BarcodeCommand { get; }

// -- New filter/sort/search state --

[ObservableProperty]
private string searchText = string.Empty;

[ObservableProperty]
private bool isSearchExpanded;

[ObservableProperty]
private bool isFilterExpanded;

[ObservableProperty]
private bool isSortExpanded;

[ObservableProperty]
private ProductFilterMode activeFilter;

[ObservableProperty]
private ProductSortMode activeSort;

// -- End new state --
```

- [ ] **Step 3: Verify compilation**

```bash
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -20
```
Expected: Build succeeds (warnings about unused partial methods are fine — they'll be wired next).

---

### Task 2: Implement `RefreshDisplay()` with filter, search, and sort logic

**Files:**
- Modify: `FridgeScan/ViewModels/ProductsViewModel.cs`

- [ ] **Step 1: Add `RefreshDisplay()` method after the existing `RefreshGrouping()` method (around line 131)**

```csharp
public void RefreshDisplay()
{
    GroupedProducts.Clear();

    if (productsManager.Products == null)
        return;

    IEnumerable<Product> query = productsManager.Products;

    // Apply search filter
    if (!string.IsNullOrEmpty(SearchText))
    {
        query = query.Where(p =>
            p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
    }

    // Apply expiry filter
    if (ActiveFilter == ProductFilterMode.ExpiringSoon)
    {
        // Shows products expiring within 7 days OR already expired
        query = query.Where(p =>
            p.DaysUntilExpiry.HasValue && p.DaysUntilExpiry.Value <= 7);
    }
    else if (ActiveFilter == ProductFilterMode.Expired)
    {
        query = query.Where(p =>
            p.DaysUntilExpiry.HasValue && p.DaysUntilExpiry.Value < 0);
    }

    // Group by category
    var groups = query
        .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Other" : p.Category);

    // Apply sort mode
    if (ActiveSort == ProductSortMode.Alphabetical)
    {
        var sorted = groups
            .OrderBy(g => g.Key)
            .Select(g => new ListViewFoodCategory(
                g.Key,
                g.OrderBy(p => p.Name).ToList()))
            .ToList();

        foreach (var g in sorted)
            GroupedProducts.Add(g);
    }
    else // ByExpiry
    {
        var sorted = groups
            .OrderBy(g => g.Min(p => p.DaysUntilExpiry ?? int.MaxValue))
            .Select(g => new ListViewFoodCategory(
                g.Key,
                g.OrderBy(p => p.DaysUntilExpiry ?? int.MaxValue)
                 .ThenBy(p => p.Name)
                 .ToList()))
            .ToList();

        foreach (var g in sorted)
            GroupedProducts.Add(g);
    }
}
```

- [ ] **Step 2: Add partial method handlers so property changes trigger refresh**

```csharp
// Add after RefreshDisplay() — these are called automatically by [ObservableProperty]
partial void OnSearchTextChanged(string value)
{
    RefreshDisplay();
}

partial void OnActiveFilterChanged(ProductFilterMode value)
{
    RefreshDisplay();
}

partial void OnActiveSortChanged(ProductSortMode value)
{
    RefreshDisplay();
}
```

- [ ] **Step 3: Update `OnAppearing` path — modify `RefreshAfterEdit()` to use `RefreshDisplay()`**

Change the existing `RefreshAfterEdit()` method body to simply call `RefreshDisplay()`:

```csharp
/// <summary>
/// Syncs groupings after edits/deletes from detail page.
/// Now respects active filter/sort/search by calling RefreshDisplay.
/// </summary>
public void RefreshAfterEdit()
{
    RefreshDisplay();
}
```

- [ ] **Step 4: Update pull-to-refresh to preserve filter/sort/search**

In `LoadProductsAsync()`, change the last line from `RefreshGrouping()` to `RefreshDisplay()`:

```csharp
public async Task LoadProductsAsync()
{
    var items = await productService.GetProductsAsync();
    productsManager.Init(items);
    RefreshDisplay();  // was RefreshGrouping()
}
```

- [ ] **Step 5: In `AddItem()`, change `AddProductToGroups(product)` to `RefreshDisplay()`**

The `AddItem()` method adds a product to the manager then calls `AddProductToGroups(product)`. Change it to `RefreshDisplay()` instead so the new item appears respecting the current sort order:

Edit in `AddItem()`, right after the `productsManager.AddProduct(product)` / `AddProductToGroups(product)` or `group.FoodMenuCollection.Add(product)` lines — replace `AddProductToGroups(product)` with `RefreshDisplay()` in the `AddItem()` method body:

```csharp
// In AddItem(), replace:
// AddProductToGroups(product);
// with:
RefreshDisplay();
```

And in the else-branch of `AddItem()` (where the product was created new), the existing code calls `AddProductToGroups(product)` — replace with `RefreshDisplay()` too.

- [ ] **Step 6: Build to verify**

```bash
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -20
```
Expected: Build succeeds.

---

### Task 3: Restructure ProductsPage.xaml header — remove old buttons, add toolbar

**Files:**
- Modify: `FridgeScan/Views/ProductsPage.xaml`

- [ ] **Step 1: Replace the header's top-row Grid (title + search + barcode buttons) with just a title**

Find the existing `Grid ColumnDefinitions="*,Auto,Auto"` block (lines 34–85 in the original) and replace it:

```xml
<!-- Page header: title + subtitle only (no action buttons) -->
<VerticalStackLayout Spacing="4" Padding="4,0,4,0">
    <Label Text="Inventory"
           FontSize="28"
           FontAttributes="Bold"
           TextColor="White"
           CharacterSpacing="-0.3" />
    <Label Text="Keep track of products and expiry dates"
           FontSize="13"
           TextColor="#8888AA" />
</VerticalStackLayout>
```

- [ ] **Step 2: Add the action toolbar between the header and the Add Item card**

Add this after the `</VerticalStackLayout>` (the title block) and before the Add Item card `<Border>`:

```xml
<!-- Action toolbar: Search, Filter, Sort pills -->
<Grid x:Name="ToolbarGrid"
      RowDefinitions="Auto,Auto"
      Margin="4,8,4,12"
      ColumnSpacing="8">
    
    <!-- Row 0: Default pill row -->
    <Grid Grid.Row="0"
          x:Name="CollapsedToolbar"
          ColumnDefinitions="2*,*,*"
          ColumnSpacing="8">
        
        <!-- Search pill -->
        <Border Grid.Column="0"
                x:Name="SearchPill"
                BackgroundColor="#1E1E3A"
                StrokeShape="RoundRectangle 18"
                Stroke="Transparent"
                HeightRequest="36"
                Padding="14,0">
            <Border.GestureRecognizers>
                <TapGestureRecognizer Tapped="OnSearchPillTapped" />
            </Border.GestureRecognizers>
            <HorizontalStackLayout Spacing="6" VerticalOptions="Center">
                <Label Text="&#xe8b6;"
                       FontFamily="Material"
                       FontSize="14"
                       TextColor="#8888AA"
                       VerticalOptions="Center" />
                <Label Text="Search"
                       FontSize="12"
                       TextColor="#8888AA"
                       VerticalOptions="Center" />
            </HorizontalStackLayout>
        </Border>
        
        <!-- Filter pill -->
        <Border Grid.Column="1"
                x:Name="FilterPill"
                BackgroundColor="#1E1E3A"
                StrokeShape="RoundRectangle 18"
                Stroke="Transparent"
                HeightRequest="36"
                Padding="14,0">
            <Border.GestureRecognizers>
                <TapGestureRecognizer Tapped="OnFilterPillTapped" />
            </Border.GestureRecognizers>
            <HorizontalStackLayout Spacing="6" VerticalOptions="Center">
                <Label x:Name="FilterIcon"
                       Text="&#xe152;"
                       FontFamily="Material"
                       FontSize="14"
                       TextColor="#CCCCDD"
                       VerticalOptions="Center" />
                <Label x:Name="FilterLabel"
                       Text="Filter"
                       FontSize="12"
                       TextColor="#CCCCDD"
                       VerticalOptions="Center" />
                <!-- Active indicator dot -->
                <Border x:Name="FilterDot"
                        IsVisible="False"
                        WidthRequest="6"
                        HeightRequest="6"
                        BackgroundColor="#D0BCFF"
                        StrokeShape="RoundRectangle 3"
                        Stroke="Transparent"
                        VerticalOptions="Center" />
            </HorizontalStackLayout>
        </Border>
        
        <!-- Sort pill -->
        <Border Grid.Column="2"
                x:Name="SortPill"
                BackgroundColor="#1E1E3A"
                StrokeShape="RoundRectangle 18"
                Stroke="Transparent"
                HeightRequest="36"
                Padding="14,0">
            <Border.GestureRecognizers>
                <TapGestureRecognizer Tapped="OnSortPillTapped" />
            </Border.GestureRecognizers>
            <HorizontalStackLayout Spacing="6" VerticalOptions="Center">
                <Label x:Name="SortIcon"
                       Text="&#xe164;"
                       FontFamily="Material"
                       FontSize="14"
                       TextColor="#CCCCDD"
                       VerticalOptions="Center" />
                <Label x:Name="SortLabel"
                       Text="Sort"
                       FontSize="12"
                       TextColor="#CCCCDD"
                       VerticalOptions="Center" />
                <Border x:Name="SortDot"
                        IsVisible="False"
                        WidthRequest="6"
                        HeightRequest="6"
                        BackgroundColor="#D0BCFF"
                        StrokeShape="RoundRectangle 3"
                        Stroke="Transparent"
                        VerticalOptions="Center" />
            </HorizontalStackLayout>
        </Border>
    </Grid>
    
    <!-- Row 1: Expanded panels / secondary pills -->
    <Grid Grid.Row="1" x:Name="ToolbarRow1">
        
        <!-- Search expanded bar (full width) -->
        <Border x:Name="SearchExpanded"
                IsVisible="False"
                BackgroundColor="#1E1E3A"
                StrokeShape="RoundRectangle 18"
                Stroke="#D0BCFF"
                HeightRequest="36"
                Padding="12,0">
            <Grid ColumnDefinitions="Auto,*,Auto" VerticalOptions="Center">
                <Label Grid.Column="0"
                       Text="&#xe8b6;"
                       FontFamily="Material"
                       FontSize="14"
                       TextColor="#D0BCFF"
                       VerticalOptions="Center" />
                <Entry Grid.Column="1"
                       x:Name="SearchEntry"
                       Placeholder="Search products..."
                       PlaceholderColor="#666688"
                       TextColor="White"
                       FontSize="12"
                       BackgroundColor="Transparent"
                       Text="{Binding SearchText, Mode=TwoWay}"
                       VerticalOptions="Center"
                       Margin="8,0,0,0" />
                <Border Grid.Column="2"
                        x:Name="SearchDismiss"
                        BackgroundColor="Transparent"
                        StrokeShape="RoundRectangle 12"
                        Stroke="Transparent"
                        WidthRequest="24"
                        HeightRequest="24"
                        VerticalOptions="Center">
                    <Border.GestureRecognizers>
                        <TapGestureRecognizer Tapped="OnSearchDismissTapped" />
                    </Border.GestureRecognizers>
                    <Label Text="&#xe5cd;"
                           FontFamily="Material"
                           FontSize="14"
                           TextColor="#8888AA"
                           HorizontalOptions="Center"
                           VerticalOptions="Center" />
                </Border>
            </Grid>
        </Border>
        
        <!-- Secondary Filter/Sort pills (shown when search is expanded) -->
        <HorizontalStackLayout x:Name="SecondaryPills"
                                IsVisible="False"
                                Spacing="8"
                                Margin="0,6,0,0">
            <Border BackgroundColor="#1E1E3A"
                    StrokeShape="RoundRectangle 18"
                    Stroke="Transparent"
                    HeightRequest="34"
                    Padding="12,0">
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Tapped="OnSecondaryFilterTapped" />
                </Border.GestureRecognizers>
                <HorizontalStackLayout Spacing="6" VerticalOptions="Center">
                    <Label Text="&#xe152;"
                           FontFamily="Material"
                           FontSize="13"
                           TextColor="#CCCCDD" />
                    <Label Text="Filter"
                           FontSize="11"
                           TextColor="#CCCCDD" />
                </HorizontalStackLayout>
            </Border>
            <Border BackgroundColor="#1E1E3A"
                    StrokeShape="RoundRectangle 18"
                    Stroke="Transparent"
                    HeightRequest="34"
                    Padding="12,0">
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Tapped="OnSecondarySortTapped" />
                </Border.GestureRecognizers>
                <HorizontalStackLayout Spacing="6" VerticalOptions="Center">
                    <Label Text="&#xe164;"
                           FontFamily="Material"
                           FontSize="13"
                           TextColor="#CCCCDD" />
                    <Label Text="Sort"
                           FontSize="11"
                           TextColor="#CCCCDD" />
                </HorizontalStackLayout>
            </Border>
        </HorizontalStackLayout>
        
        <!-- Filter segment panel -->
        <Border x:Name="FilterPanel"
                IsVisible="False"
                BackgroundColor="#14142E"
                StrokeShape="RoundRectangle 12"
                Stroke="#2A2E58"
                Padding="4"
                HeightRequest="36"
                HorizontalOptions="Start"
                Margin="0,6,0,0">
            <HorizontalStackLayout Spacing="3" VerticalOptions="Center">
                <Border x:Name="FilterSegmentExpiring"
                        BackgroundColor="#2A2E58"
                        StrokeShape="RoundRectangle 9"
                        Stroke="Transparent"
                        Padding="12,7">
                    <Border.GestureRecognizers>
                        <TapGestureRecognizer Tapped="OnFilterSegmentExpiringTapped" />
                    </Border.GestureRecognizers>
                    <Label x:Name="FilterLabelExpiring"
                           Text="Expiring soon"
                           FontSize="11"
                           TextColor="White"
                           VerticalOptions="Center" />
                </Border>
                <Border x:Name="FilterSegmentExpired"
                        BackgroundColor="Transparent"
                        StrokeShape="RoundRectangle 9"
                        Stroke="Transparent"
                        Padding="12,7">
                    <Border.GestureRecognizers>
                        <TapGestureRecognizer Tapped="OnFilterSegmentExpiredTapped" />
                    </Border.GestureRecognizers>
                    <Label x:Name="FilterLabelExpired"
                           Text="Expired"
                           FontSize="11"
                           TextColor="#8888AA"
                           VerticalOptions="Center" />
                </Border>
                <Border x:Name="FilterSegmentAll"
                        BackgroundColor="Transparent"
                        StrokeShape="RoundRectangle 9"
                        Stroke="Transparent"
                        Padding="12,7">
                    <Border.GestureRecognizers>
                        <TapGestureRecognizer Tapped="OnFilterSegmentAllTapped" />
                    </Border.GestureRecognizers>
                    <Label x:Name="FilterLabelAll"
                           Text="All"
                           FontSize="11"
                           TextColor="#8888AA"
                           VerticalOptions="Center" />
                </Border>
            </HorizontalStackLayout>
        </Border>
        
        <!-- Sort segment panel -->
        <Border x:Name="SortPanel"
                IsVisible="False"
                BackgroundColor="#14142E"
                StrokeShape="RoundRectangle 12"
                Stroke="#2A2E58"
                Padding="4"
                HeightRequest="36"
                HorizontalOptions="End"
                Margin="0,6,0,0">
            <HorizontalStackLayout Spacing="3" VerticalOptions="Center">
                <Border x:Name="SortSegmentAZ"
                        BackgroundColor="#2A2E58"
                        StrokeShape="RoundRectangle 9"
                        Stroke="Transparent"
                        Padding="12,7">
                    <Border.GestureRecognizers>
                        <TapGestureRecognizer Tapped="OnSortSegmentAZTapped" />
                    </Border.GestureRecognizers>
                    <Label x:Name="SortLabelAZ"
                           Text="A-Z"
                           FontSize="11"
                           TextColor="White"
                           VerticalOptions="Center" />
                </Border>
                <Border x:Name="SortSegmentExpiry"
                        BackgroundColor="Transparent"
                        StrokeShape="RoundRectangle 9"
                        Stroke="Transparent"
                        Padding="12,7">
                    <Border.GestureRecognizers>
                        <TapGestureRecognizer Tapped="OnSortSegmentExpiryTapped" />
                    </Border.GestureRecognizers>
                    <Label x:Name="SortLabelExpiry"
                           Text="By expiry"
                           FontSize="11"
                           TextColor="#8888AA"
                           VerticalOptions="Center" />
                </Border>
            </HorizontalStackLayout>
        </Border>
    </Grid>
</Grid>
```

- [ ] **Step 3: Add the floating barcode FAB inside `mainGrid` (the SfPullToRefresh's pullable content)**

Find the `<Grid x:Name="mainGrid">` inside the SfPullToRefresh (around line 150 in original). Add the FAB as the last child inside this Grid:

```xml
<Grid x:Name="mainGrid">
    <sf:SfListView
        x:Name="listView"
        x:DataType="vm:ProductsViewModel"
        AutoFitMode="DynamicHeight"
        IsStickyGroupHeader="True"
        ItemsSource="{Binding GroupedProducts}"
        SelectionMode="None">
        <!-- ... existing template ... -->
    </sf:SfListView>

    <!-- Floating barcode FAB -->
    <Border x:Name="BarcodeFab"
            BackgroundColor="#D0BCFF"
            StrokeShape="RoundRectangle 28"
            Stroke="Transparent"
            WidthRequest="56"
            HeightRequest="56"
            VerticalOptions="End"
            HorizontalOptions="End"
            Margin="0,0,16,16"
            InputTransparent="False"
            ZIndex="100">
        <Border.GestureRecognizers>
            <TapGestureRecognizer Command="{Binding BarcodeCommand}" />
        </Border.GestureRecognizers>
        <Label Text="&#xef39;"
               FontFamily="Material"
               FontSize="22"
               TextColor="#0D0D2B"
               HorizontalOptions="Center"
               VerticalOptions="Center" />
    </Border>
</Grid>
```

- [ ] **Step 4: Build to verify XAML syntax**

```bash
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -20
```
Expected: Build succeeds (warnings about unused code-behind handlers are fine).

---

### Task 4: Add code-behind event handlers for all chip interactions

**Files:**
- Modify: `FridgeScan/Views/ProductsPage.xaml.cs`

- [ ] **Step 1: Add fields and ensure `OnLoaded` still works**

Add a field at class level to track whether the ViewModel is expanded:

```csharp
public partial class ProductsPage : ContentPage
{
    private bool isSearchAnimating;

    public ProductsPage()
    {
        // ... existing constructor ...
    }
```

- [ ] **Step 2: Add `OnSearchPillTapped` — expand search with animation**

```csharp
private async void OnSearchPillTapped(object sender, EventArgs e)
{
    if (isSearchAnimating) return;
    isSearchAnimating = true;

    if (BindingContext is ProductsViewModel vm)
    {
        // Collapse any open filter/sort panels
        vm.IsFilterExpanded = false;
        vm.IsSortExpanded = false;

        // Show search expanded, hide collapsed pills
        SearchExpanded.IsVisible = true;
        SearchExpanded.Opacity = 0;

        // Fade out the collapsed toolbar pills
        await CollapsedToolbar.FadeTo(0, 150, Easing.CubicIn);

        // Show the search bar
        await SearchExpanded.FadeTo(1, 200, Easing.CubicOut);

        // Show secondary pills (Filter + Sort on second row)
        SecondaryPills.IsVisible = true;
        SecondaryPills.Opacity = 0;
        await SecondaryPills.FadeTo(1, 200, Easing.CubicOut);

        CollapsedToolbar.IsVisible = false;
        vm.IsSearchExpanded = true;

        // Focus the search entry and show keyboard
        SearchEntry.Focus();
    }

    isSearchAnimating = false;
}
```

- [ ] **Step 3: Add `OnSearchDismissTapped` — collapse search**

```csharp
private async void OnSearchDismissTapped(object sender, EventArgs e)
{
    if (isSearchAnimating) return;
    isSearchAnimating = true;

    if (BindingContext is ProductsViewModel vm)
    {
        // Clear search text
        vm.SearchText = string.Empty;
        SearchEntry.Unfocus();

        // Hide secondary pills
        await SecondaryPills.FadeTo(0, 150, Easing.CubicIn);
        SecondaryPills.IsVisible = false;

        // Hide search bar
        await SearchExpanded.FadeTo(0, 150, Easing.CubicIn);
        SearchExpanded.IsVisible = false;

        // Show collapsed pills
        CollapsedToolbar.IsVisible = true;
        CollapsedToolbar.Opacity = 0;
        await CollapsedToolbar.FadeTo(1, 200, Easing.CubicOut);

        vm.IsSearchExpanded = false;
    }

    isSearchAnimating = false;
}
```

- [ ] **Step 4: Add `OnFilterPillTapped` and `OnSecondaryFilterTapped` — toggle filter panel**

```csharp
private void OnFilterPillTapped(object sender, EventArgs e)
{
    ToggleFilterPanel();
}

private void OnSecondaryFilterTapped(object sender, EventArgs e)
{
    ToggleFilterPanel();
}

private void ToggleFilterPanel()
{
    if (BindingContext is ProductsViewModel vm)
    {
        // Close sort panel if open
        vm.IsSortExpanded = false;
        SortPanel.IsVisible = false;

        // Toggle filter panel
        vm.IsFilterExpanded = !vm.IsFilterExpanded;
        FilterPanel.IsVisible = vm.IsFilterExpanded;
        FilterPill.BackgroundColor = vm.IsFilterExpanded
            ? Color.FromArgb("#2A2E58")
            : Color.FromArgb("#1E1E3A");
    }
}
```

- [ ] **Step 5: Add `OnSortPillTapped` and `OnSecondarySortTapped` — toggle sort panel**

```csharp
private void OnSortPillTapped(object sender, EventArgs e)
{
    ToggleSortPanel();
}

private void OnSecondarySortTapped(object sender, EventArgs e)
{
    ToggleSortPanel();
}

private void ToggleSortPanel()
{
    if (BindingContext is ProductsViewModel vm)
    {
        // Close filter panel if open
        vm.IsFilterExpanded = false;
        FilterPanel.IsVisible = false;

        // Toggle sort panel
        vm.IsSortExpanded = !vm.IsSortExpanded;
        SortPanel.IsVisible = vm.IsSortExpanded;
        SortPill.BackgroundColor = vm.IsSortExpanded
            ? Color.FromArgb("#2A2E58")
            : Color.FromArgb("#1E1E3A");
    }
}
```

- [ ] **Step 6: Add filter segment selection handlers**

```csharp
private void OnFilterSegmentExpiringTapped(object sender, EventArgs e)
{
    if (BindingContext is ProductsViewModel vm)
    {
        vm.ActiveFilter = ProductFilterMode.ExpiringSoon;
        UpdateFilterPillAppearance(vm);
        ClosePanels();
    }
}

private void OnFilterSegmentExpiredTapped(object sender, EventArgs e)
{
    if (BindingContext is ProductsViewModel vm)
    {
        vm.ActiveFilter = ProductFilterMode.Expired;
        UpdateFilterPillAppearance(vm);
        ClosePanels();
    }
}

private void OnFilterSegmentAllTapped(object sender, EventArgs e)
{
    if (BindingContext is ProductsViewModel vm)
    {
        vm.ActiveFilter = ProductFilterMode.None;
        UpdateFilterPillAppearance(vm);
        ClosePanels();
    }
}

private void UpdateFilterPillAppearance(ProductsViewModel vm)
{
    bool isActive = vm.ActiveFilter != ProductFilterMode.None;
    FilterDot.IsVisible = isActive;
    FilterLabel.Text = vm.ActiveFilter switch
    {
        ProductFilterMode.ExpiringSoon => "Expiring",
        ProductFilterMode.Expired => "Expired",
        _ => "Filter"
    };
    FilterIcon.TextColor = isActive
        ? Color.FromArgb("#D0BCFF")
        : Color.FromArgb("#CCCCDD");
    FilterLabel.TextColor = isActive
        ? Color.FromArgb("#D0BCFF")
        : Color.FromArgb("#CCCCDD");
    FilterPill.BackgroundColor = isActive
        ? Color.FromArgb("#2A2E58")
        : Color.FromArgb("#1E1E3A");
}
```

- [ ] **Step 7: Add sort segment selection handlers**

```csharp
private void OnSortSegmentAZTapped(object sender, EventArgs e)
{
    if (BindingContext is ProductsViewModel vm)
    {
        vm.ActiveSort = ProductSortMode.Alphabetical;
        UpdateSortPillAppearance(vm);
        ClosePanels();
    }
}

private void OnSortSegmentExpiryTapped(object sender, EventArgs e)
{
    if (BindingContext is ProductsViewModel vm)
    {
        vm.ActiveSort = ProductSortMode.ByExpiry;
        UpdateSortPillAppearance(vm);
        ClosePanels();
    }
}

private void UpdateSortPillAppearance(ProductsViewModel vm)
{
    bool isActive = vm.ActiveSort != ProductSortMode.Alphabetical;
    SortDot.IsVisible = isActive;
    SortLabel.Text = isActive ? "Expiry" : "Sort";
    SortIcon.TextColor = isActive
        ? Color.FromArgb("#D0BCFF")
        : Color.FromArgb("#CCCCDD");
    SortLabel.TextColor = isActive
        ? Color.FromArgb("#D0BCFF")
        : Color.FromArgb("#CCCCDD");
    SortPill.BackgroundColor = isActive
        ? Color.FromArgb("#2A2E58")
        : Color.FromArgb("#1E1E3A");
}
```

- [ ] **Step 8: Add `ClosePanels()` helper to collapse all panels**

```csharp
private void ClosePanels()
{
    if (BindingContext is ProductsViewModel vm)
    {
        vm.IsFilterExpanded = false;
        vm.IsSortExpanded = false;
        FilterPanel.IsVisible = false;
        SortPanel.IsVisible = false;
        FilterPill.BackgroundColor = Color.FromArgb("#1E1E3A");
        SortPill.BackgroundColor = Color.FromArgb("#1E1E3A");
    }
}
```

- [ ] **Step 9: Update the existing `OnSearchTapped` method — repurpose or remove**

The old `OnSearchTapped` focused the autocomplete. Remove that method and rename it or remove it entirely. The search pill now handles search. Find and replace the old method:

Remove the existing `OnSearchTapped` method entirely (it was a code-behind handler for the old header search button). The autocomplete already focuses when tapped by the user.

- [ ] **Step 10: Build to verify all handlers compile**

```bash
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -20
```
Expected: Build succeeds.

---

### Task 5: Clean up segment panel visual states (highlight selected segment)

**Files:**
- Modify: `FridgeScan/Views/ProductsPage.xaml.cs`

- [ ] **Step 1: Add a helper to update segment selection highlighting**

This should be called from each segment selection handler so Appearing-init also restores the correct state:

```csharp
private void UpdateFilterSegmentHighlight()
{
    if (BindingContext is not ProductsViewModel vm) return;

    bool isExpiring = vm.ActiveFilter == ProductFilterMode.ExpiringSoon;
    bool isExpired = vm.ActiveFilter == ProductFilterMode.Expired;
    bool isAll = vm.ActiveFilter == ProductFilterMode.None;

    FilterSegmentExpiring.BackgroundColor = isExpiring
        ? Color.FromArgb("#2A2E58") : Color.FromArgb("Transparent");
    FilterLabelExpiring.TextColor = isExpiring ? Colors.White : Color.FromArgb("#8888AA");

    FilterSegmentExpired.BackgroundColor = isExpired
        ? Color.FromArgb("#2A2E58") : Color.FromArgb("Transparent");
    FilterLabelExpired.TextColor = isExpired ? Colors.White : Color.FromArgb("#8888AA");

    FilterSegmentAll.BackgroundColor = isAll
        ? Color.FromArgb("#2A2E58") : Color.FromArgb("Transparent");
    FilterLabelAll.TextColor = isAll ? Colors.White : Color.FromArgb("#8888AA");
}

private void UpdateSortSegmentHighlight()
{
    if (BindingContext is not ProductsViewModel vm) return;

    bool isAZ = vm.ActiveSort == ProductSortMode.Alphabetical;
    bool isExpiry = vm.ActiveSort == ProductSortMode.ByExpiry;

    SortSegmentAZ.BackgroundColor = isAZ
        ? Color.FromArgb("#2A2E58") : Color.FromArgb("Transparent");
    SortLabelAZ.TextColor = isAZ ? Colors.White : Color.FromArgb("#8888AA");

    SortSegmentExpiry.BackgroundColor = isExpiry
        ? Color.FromArgb("#2A2E58") : Color.FromArgb("Transparent");
    SortLabelExpiry.TextColor = isExpiry ? Colors.White : Color.FromArgb("#8888AA");
}
```

- [ ] **Step 2: Call these helpers from segment tap handlers and load**

Update each filter segment handler to call `UpdateFilterSegmentHighlight()` after setting the mode:

```csharp
private void OnFilterSegmentExpiringTapped(object sender, EventArgs e)
{
    if (BindingContext is ProductsViewModel vm)
    {
        vm.ActiveFilter = ProductFilterMode.ExpiringSoon;
        UpdateFilterPillAppearance(vm);
        UpdateFilterSegmentHighlight();
        ClosePanels();
    }
}

// Same for OnFilterSegmentExpiredTapped and OnFilterSegmentAllTapped
```

Update each sort segment handler similarly:

```csharp
private void OnSortSegmentAZTapped(object sender, EventArgs e)
{
    if (BindingContext is ProductsViewModel vm)
    {
        vm.ActiveSort = ProductSortMode.Alphabetical;
        UpdateSortPillAppearance(vm);
        UpdateSortSegmentHighlight();
        ClosePanels();
    }
}

// Same for OnSortSegmentExpiryTapped
```

- [ ] **Step 3: Restore pill states when page appears**

In `OnAppearing`, after `vm.RefreshAfterEdit()`, restore the visual states:

```csharp
protected override void OnAppearing()
{
    base.OnAppearing();
    if (BindingContext is ProductsViewModel vm)
    {
        vm.RefreshAfterEdit();
        UpdateFilterPillAppearance(vm);
        UpdateSortPillAppearance(vm);

        // Hide panels on return
        vm.IsFilterExpanded = false;
        vm.IsSortExpanded = false;
        FilterPanel.IsVisible = false;
        SortPanel.IsVisible = false;
    }
}
```

- [ ] **Step 4: Build to verify**

```bash
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -20
```
Expected: Build succeeds.

---

### Task 6: Final review and edge case check

**Files:**
- Review: `FridgeScan/Views/ProductsPage.xaml`
- Review: `FridgeScan/Views/ProductsPage.xaml.cs`
- Review: `FridgeScan/ViewModels/ProductsViewModel.cs`

- [ ] **Step 1: Verify pull-to-refresh works with search open**

In `pullToRefresh_Refreshing`, the code calls `LoadProductsAsync()`. Task 2 updated `LoadProductsAsync` to call `RefreshDisplay()` instead of `RefreshGrouping()`, so the active filter/sort/search will be preserved automatically. Verify the code:

```csharp
// Should already look like this in ProductsPage.xaml.cs:
private async void pullToRefresh_Refreshing(object sender, EventArgs e)
{
    pullToRefresh.IsRefreshing = true;
    try
    {
        await ((ProductsViewModel)BindingContext).LoadProductsAsync();
    }
    finally
    {
        pullToRefresh.IsRefreshing = false;
    }
}
```

No changes needed here.

- [ ] **Step 2: Verify FAB doesn't overlap with FloatingBottomBar**

The `mainGrid` is inside `SfPullToRefresh.PullableContent`, which is inside the `Grid.Row="1"` of the outer Grid. The `FloatingBottomBar` is at `Grid.Row="1"` of the outer Grid (the Auto row at the bottom). Since the `mainGrid` sits above the `FloatingBottomBar` in layout, the FAB at the bottom of `mainGrid` should sit just above the `FloatingBottomBar`. The `Margin="0,0,16,16"` on the FAB provides 16dp padding from its container bottom. This should look right, but verify by checking spacing in the emulator.

- [ ] **Step 3: Verify FAB tap target**

The FAB is 56×56dp with a visible icon. Confirm that `InputTransparent="False"` is set so taps register. Also confirm the `sf:SfListView` above it doesn't capture taps (it has `SelectionMode="None"` so it won't).

- [ ] **Step 4: Review all Material 3 color tokens used**

Check all new colors in XAML match the CLAUDE.md spec:

| Element | Token | Value |
|---------|-------|-------|
| Page background | — | `#0D1023` (unchanged) |
| Pill background | Action surface | `#1E1E3A` |
| Pill active background | — | `#2A2E58` |
| Pill text (default) | Secondary text | `#CCCCDD` |
| Pill text (inactive) | Muted text | `#8888AA` |
| Pill active text | Primary accent | `#D0BCFF` |
| FAB background | Primary container | `#D0BCFF` |
| FAB icon | Page background | `#0D0D2B` |
| Segment panel | Card surface | `#14142E` |
| Segment active | — | `#2A2E58` |

- [ ] **Step 5: Build final version and check warnings**

```bash
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1
```
Expected: Build succeeds with no errors. Warnings about unused variables/behaviours are OK.

---

### Summary of all files changed

| File | Changes |
|------|---------|
| `FridgeScan/ViewModels/ProductsViewModel.cs` | Add `ProductFilterMode` enum, `ProductSortMode` enum, 6 `[ObservableProperty]` fields, `RefreshDisplay()` method, partial change handlers. Update `LoadProductsAsync()`, `RefreshAfterEdit()`, and `AddItem()` to call `RefreshDisplay()`. |
| `FridgeScan/Views/ProductsPage.xaml` | Remove search and barcode buttons from header. Add action toolbar Grid with pills, segment panels, search bar. Add barcode FAB to mainGrid. |
| `FridgeScan/Views/ProductsPage.xaml.cs` | Add handlers: `OnSearchPillTapped`, `OnSearchDismissTapped`, `OnFilterPillTapped`, `OnSortPillTapped`, `OnSecondaryFilterTapped`, `OnSecondarySortTapped`, `OnFilterSegmentExpiringTapped`, `OnFilterSegmentExpiredTapped`, `OnFilterSegmentAllTapped`, `OnSortSegmentAZTapped`, `OnSortSegmentExpiryTapped`. Helpers: `ToggleFilterPanel`, `ToggleSortPanel`, `ClosePanels`, `UpdateFilterPillAppearance`, `UpdateSortPillAppearance`, `UpdateFilterSegmentHighlight`, `UpdateSortSegmentHighlight`. Update `OnAppearing`. Remove old `OnSearchTapped`. |
