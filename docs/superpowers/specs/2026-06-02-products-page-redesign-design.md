# Products Page Redesign — Design Spec

## Overview

Redesign the Products page to align with the project's Material 3 expressive dark theme design system, add expiry date and frozen tracking, replace quantity +/- controls with an edit button that navigates to a detail page, and introduce color-coded expiry badges.

## Product Model Changes

### New fields (`FridgeScan/Models/Product.cs`)

```csharp
[ObservableProperty] private DateTime? expiryDate;
[ObservableProperty] private bool isFrozen;
```

### Removed commands

- `DecreaseCommand` — replaced by edit → detail page
- `IncreaseCommand` — replaced by edit → detail page
- `RemoveCommand` — moved to detail page

### Computed properties

```csharp
public int? DaysUntilExpiry =>
    expiryDate.HasValue
        ? (int?)(expiryDate.Value.Date - DateTime.Today.Date).TotalDays
        : null;

public bool ShowExpiryBadge =>
    DaysUntilExpiry.HasValue && DaysUntilExpiry.Value <= 3;

public string ExpiryDisplayText => DaysUntilExpiry switch
{
    < 0 => "Expired",
    0   => "Today",
    <= 3 => $"{DaysUntilExpiry}d left",
    _   => null
};

public Color ExpiryColor => DaysUntilExpiry switch
{
    < 0 => Color.FromArgb("#E74C3C"),   // Red
    _   => Color.FromArgb("#F39C12"),   // Amber
};

public bool ShowFrozenIcon => isFrozen;
```

Days 4+ or null expiry → badge hidden. No green badge used.

## ProductsPage Changes (`FridgeScan/Views/ProductsPage.xaml`)

### Page background
- `#0D0D2B` — matching M3 design system

### Header (unchanged)
- `SfAutocomplete` for adding items
- Barcode scan icon button

### Category group headers (Option A — subtle section label)
- Small uppercase muted text (`#8888AA`, 12sp, semibold, letter-spacing)
- Item count in `#666688`
- ▼ chevron for expand/collapse

### Product row inside each category
- Single horizontal row, `CornerRadius="12"`, `BackgroundColor="#14142E"`
- Layout (left → right): `Name` (flex) — `❄️` frozen icon (if frozen) — expiry pill — edit circle button
- **Name**: 14sp, White, Roboto-Regular
- **Frozen icon**: Material `ac_unit` codepoint, `#8888AA`
- **Expiry pill**: 11sp, white, bold, rounded `CornerRadius="10"`, `Padding="4,2,8,2"`, background = red (`#E74C3C`) or amber (`#F39C12`)
- **Edit button**: 40×40 circle, `BackgroundColor="#1E1E3A"`, `StrokeShape="RoundRectangle 20"`, Material icon `&#xe3c9;` at 18sp, `TextColor="#CCCCDD"`

### Removed from template
- Minus button (Grid.Column 2, Material `remove` icon)
- Quantity label (Grid.Column 3)
- Plus button (Grid.Column 4, Material `add` icon)
- Delete button (Grid.Column 5, Material `delete` icon)
- `BoxView` divider below category header
- All associated commands (`DecreaseCommand`, `IncreaseCommand`, `RemoveCommand`)

### List control
- Keep `SfListView` with `IsStickyGroupHeader="True"` for expandable categories

## ProductDetailPage (new)

### Files
- `FridgeScan/Views/ProductDetailPage.xaml` + `.xaml.cs`
- `FridgeScan/ViewModels/ProductDetailViewModel.cs`

### Layout
- `BackgroundColor="#0D0D2B"`
- Header: back arrow (Material `&#xe5c4;`) + "Edit Product" title
- Form fields, each in a `#14142E` rounded surface:
  1. **Name** — `Entry`, white text
  2. **Quantity** — −/+/number stepper (`#1E1E3A` circle buttons)
  3. **Expiry Date** — `DatePicker` with ✕ clear button + "Clear expiry date" link to set null
  4. **Is Frozen** — `Switch` with label
  5. **Save** — pill button (`#1E1E3A`, `#CCCCDD` text)
  6. **Delete Product** — destructive pill button (`#2A1E1E`, `#ff6b6b` text)

### Behavior
- Save: calls `ProductService.UpdateProductAsync()` with all fields, pops navigation
- Delete: confirmation dialog → calls `ProductService.DeleteProductAsync()` → pops to root
- Clear expiry: sets `ExpiryDate = null`

### DI Registration
- `ProductDetailViewModel` — transient
- `ProductDetailPage` — transient

### Navigation
- Route registered in `AppShell.xaml.cs`
- Navigate: `Shell.Current.GoToAsync("ProductDetailPage", new { productId = rowId })`
- Triggered by edit icon `TapGestureRecognizer` on each product row

## ProductService Changes

### Reading from Appwrite
- Map `expiry` (DateTime?) and `frozen` (bool) columns from `AppwriteRow` data
- Update `AppwriteRow` class if needed to include these fields

### Writing to Appwrite
- Include `expiry` and `frozen` in `AddProductAsync` and `UpdateProductAsync` request bodies
- Handle null expiry (omit or send null)

## Data Flow

1. User taps edit icon on a product row
2. Navigates to `ProductDetailPage` with `productId` query parameter
3. `ProductDetailViewModel` resolves product from `ProductsManager` by `RowId`
4. On save: `ProductService.UpdateProductAsync()` → updates Appwrite → updates in-memory collection
5. On delete: confirmation → `ProductService.DeleteProductAsync()` → removes from Appwrite + in-memory collection → navigates back
6. On expiry clear: sets `ExpiryDate = null` → saves to Appwrite

## Files Changed / Created

| File | Action |
|------|--------|
| `FridgeScan/Models/Product.cs` | Modify — add fields, remove commands, add computed properties |
| `FridgeScan/Views/ProductsPage.xaml` | Modify — M3 redesign, new row layout |
| `FridgeScan/Views/ProductsPage.xaml.cs` | Modify — minor (keep as-is unless changes needed) |
| `FridgeScan/ViewModels/ProductsViewModel.cs` | Modify — remove quantity change handlers if unused |
| `FridgeScan/Services/ProductService.cs` | Modify — map expiry + frozen fields |
| `FridgeScan/Views/ProductDetailPage.xaml` | **New** |
| `FridgeScan/Views/ProductDetailPage.xaml.cs` | **New** |
| `FridgeScan/ViewModels/ProductDetailViewModel.cs` | **New** |
| `FridgeScan/AppShell.xaml.cs` | Modify — register detail route |
| `FridgeScan/MauiProgram.cs` | Modify — register new DI types |
