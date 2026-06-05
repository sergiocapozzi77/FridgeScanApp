# Products Page — Action Toolbar & Floating Barcode Button

## Overview

Add a compact action toolbar between the page header and the Add Item card, plus a floating barcode FAB in the bottom-right corner of the product list. The toolbar provides Search (expandable text field), Filter (expiring/expired), and Sort (A-Z / by expiry) via expandable segmented chips.

## Layout hierarchy

```
┌──────────────────────────────┐
│  Inventory                   │  ← Header (title + subtitle only)
│                              │
│  🔍 Search  ⬇ Filter  ↕ Sort │  ← Action toolbar (3 chips)
│                              │
│  ┌─ + Add item... ──────────┐│  ← Add Item autocomplete (unchanged)
│  └──────────────────────────┘│
│                              │
│  DAIRY (3 items)  ▸         │
│  ┌ Milk ── Expired ── ✎ ─┐ │
│  └────────────────────────┘ │
│  ┌ Cheese ─── 6d left ─ ✎ ┐│
│  └────────────────────────┘ │
│               ┌─────┐       │
│               │ 📷  │       │  ← FAB (56dp, floating bottom-right)
│               └─────┘       │
├──────────────────────────────┤
│  [Products] [Recipe] [...]   │  ← Shell tab bar (via FloatingBottomBar)
└──────────────────────────────┘
```

## Components

### 1. Floating Barcode FAB

- **Position**: Absolute/overlay in bottom-right of the product list area (above the `FloatingBottomBar`)
- **Size**: 56×56dp (was 40×40) — Material 3 FAB spec
- **Shape**: Circle (RoundRectangle 28)
- **Colors**: `BackgroundColor="#D0BCFF"` (primary container), icon `#0D0D2B`
- **Icon**: barcode material icon at FontSize 22
- **Shadow**: slight elevation via 4dp margin offset and tonal contrast
- **Command**: bound to existing `BarcodeCommand`
- **Behavior**: floats above the SfListView content, not part of the scroll

### 2. Action Toolbar (3 chips)

Placed in a horizontal StackLayout between the header and the Add Item card.

**Resting appearance:**
- 3 pills in a horizontal row, each: `BackgroundColor="#1E1E3A"`, `CornerRadius=18`, padding 7×14
- FontSize 12, text color `#CCCCDD` (or `#8888AA` when inactive)
- Search pill is flex:2 (takes more space), Filter and Sort are flex:1

**Search chip behavior:**
- Tap → Search pill animates (LayoutTo or custom) to expand to full-width of the toolbar area
- Shows a Border with Entry inside, with `🔍` icon prefix and `✕` dismiss button
- Filter & Sort pills drop to a second row below the search field
- Typing filters the product list by name (case-insensitive contains)
- Tap `✕` or clear text → collapses back to pill, Filter & Sort return to first row

**Filter chip behavior:**
- Tap → Filter pill highlights (`BackgroundColor="#2A2E58"`, `border: 1px #D0BCFF`)
- A small segmented panel drops in directly below the Filter pill, left-aligned
- 3 segments: `[Expiring soon]` | `[Expired]` | `[All]`
- Selected segment: `BackgroundColor="#2A2E58"`, white text; inactive: `#8888AA`
- Selecting an option collapses the panel, Filter pill stays highlighted with a dot indicator
- Tapping the active Filter pill again collapses without changing
- Tapping Filter while Sort panel is open → closes Sort, opens Filter

**Sort chip behavior:**
- Same pattern as Filter but right-aligned under the Sort pill
- 2 segments: `[A-Z]` | `[By expiry]`
- Same collapse/expand interaction as Filter
- Only one panel (Filter or Sort) can be open at a time

### 3. Search expand animation

- **Entry**: `LayoutTo` animation on the Search border from pill width → full available width (e.g., 120px → screen width minus padding), ~250ms easing `CubicInOut`
- Filter/Sort pills fade-shift to the second row via `TranslateTo` and `FadeTo`
- **Exit**: reverse animation on ✕ tap or when the Entry loses focus and is empty
- **Fallback**: if animation is not smooth on the target platform, use `IsVisible` toggle with a short opacity fade (200ms)

## ViewModel additions (ProductsViewModel)

### New observable properties

```csharp
[ObservableProperty] private string searchText;           // bound to search Entry
[ObservableProperty] private bool isSearchExpanded;       // controls search bar visibility
[ObservableProperty] private bool isFilterExpanded;       // Filter panel open
[ObservableProperty] private bool isSortExpanded;         // Sort panel open
[ObservableProperty] private ProductFilterMode activeFilter;  // None, ExpiringSoon, Expired
[ObservableProperty] private ProductSortMode activeSort;     // Alphabetical, ByExpiry
```

### Filter enum

```csharp
public enum ProductFilterMode { None, ExpiringSoon, Expired }
```

### Sort enum

```csharp
public enum ProductSortMode { Alphabetical, ByExpiry }
```

### Core methods

```csharp
partial void OnSearchTextChanged(string value);     // triggers RefreshDisplay()
partial void OnActiveFilterChanged(ProductFilterMode value);  // triggers RefreshDisplay()
partial void OnActiveSortChanged(ProductSortMode value);      // triggers RefreshDisplay()
```

### RefreshDisplay() logic

The existing `RefreshGrouping()` is replaced / enhanced by `RefreshDisplay()` which applies filter then sort:

1. **Start**: get all products from `productsManager.Products`
2. **Apply search filter**: if `searchText` is not empty, `.Where(p => p.Name.Contains(searchText, OrdinalIgnoreCase))`
3. **Apply expiry filter**:
   - `ExpiringSoon`: `.Where(p => p.DaysUntilExpiry <= 7)` (expiring within 7 days or already expired)
   - `Expired`: `.Where(p => p.DaysUntilExpiry < 0)`
   - `None`: no filter
4. **Group** by category (same as now)
5. **Sort groups and products**:
   - `Alphabetical`: groups ordered by category name A-Z, products within groups ordered by name A-Z
   - `ByExpiry`: groups ordered by earliest `DaysUntilExpiry` within the group (nulls last), products within groups ordered by `DaysUntilExpiry` ascending (nulls last)
6. **Update `GroupedProducts`**: in-place diff to preserve scroll position (same pattern as `RefreshAfterEdit()`)

### Save button active pill indicator

- When a filter is active (not `None`), the Filter pill gets a small dot or color change
- When sort is `ByExpiry`, the Sort pill shows `📅` instead of `↕`
- The active filter/sort is preserved when Search expands/collapses

## XAML changes (ProductsPage.xaml)

### Removed
- Existing Search button (40dp circle) from header
- Existing Barcode button (40dp circle) from header

### Added to header
- Just the title and subtitle (no action buttons)

### Added after header, before Add Item
```
<!-- Action toolbar -->
<HorizontalStackLayout Spacing="8" Padding="4,8,4,4">
  <!-- Search pill -->
  <Border x:Name="SearchPill" ... />
  <!-- Filter pill -->
  <Border x:Name="FilterPill" ... />
  <!-- Sort pill -->
  <Border x:Name="SortPill" ... />
</HorizontalStackLayout>

<!-- Filter/Sort segment panels (initially hidden, positioned absolutely or using Grid rows) -->
<Border x:Name="FilterPanel" IsVisible="{Binding IsFilterExpanded}" ... />
<Border x:Name="SortPanel" IsVisible="{Binding IsSortExpanded}" ... />
```

### Added to list area
```
<!-- Floating barcode FAB -->
<Border x:Name="BarcodeFab"
        BackgroundColor="#D0BCFF"
        StrokeShape="RoundRectangle 28"
        WidthRequest="56" HeightRequest="56"
        HorizontalOptions="End" VerticalOptions="End"
        Margin="0,0,16,16"
        InputTransparent="False">
  ...Command="{Binding BarcodeCommand}"...
</Border>
```

## Edge cases

- **Empty filter result**: show the existing empty state (already handled by empty `GroupedProducts`)
- **All products have no expiry**: in ByExpiry sort, products without expiry appear at the end
- **Filter + Search combined**: both apply simultaneously — search text filters the set, then filter mode further narrows
- **Pull-to-refresh**: should preserve the current filter/sort/search state
- **Back from detail page**: `OnAppearing` calls `RefreshAfterEdit` — need to ensure it respects active filter/sort (or relay `RefreshDisplay`)
- **Animation on slow devices**: animation degrades gracefully — panels just appear/disappear via IsVisible if LayoutTo is not supported smoothly

## Files changed

| File | Change |
|------|--------|
| `FridgeScan/ViewModels/ProductsViewModel.cs` | Add filter/sort/search properties, enums, `RefreshDisplay()` method |
| `FridgeScan/Views/ProductsPage.xaml` | Restructure header, add toolbar, add FAB, remove old buttons |
| `FridgeScan/Views/ProductsPage.xaml.cs` | Add event handlers for chip taps, search animation, segment selection |
