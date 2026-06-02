# Cookbook Detail Page Redesign

## Summary

Restructure `CookbookDetailPage.xaml` with a modern Material 3 dark-themed layout. Replace the pill-shaped back button with a minimal back arrow, move rename/delete to icon buttons in the top-right, remove the divider, and refine card styling — all while keeping existing bindings and code-behind logic intact.

## Layout Structure

**Before**: `Grid RowDefinitions="Auto,Auto,*"` — header stack, divider BoxView, CollectionView.

**After**: `Grid RowDefinitions="Auto,*"` — header row, CollectionView. No third row for the divider.

```
┌──────────────────────────────┐
│ ←  Italian Favorites    ✎ 🗑 │  Row 0: Header (Auto)
│    12 recipes                 │
│                               │  (whitespace gap, ~8px)
│ ┌───────────────────────────┐ │
│ │ 🍝 Spaghetti Carbonara  ⋯│ │  Row 1: CollectionView (*)
│ │    BBC Good Food          │ │
│ │    30 min · Easy          │ │
│ └───────────────────────────┘ │
│ ┌───────────────────────────┐ │
│ │ 🍕 Margherita Pizza     ⋯│ │
│ └───────────────────────────┘ │
└──────────────────────────────┘
```

## Header

A single horizontal row containing:

| Position | Element | Spec |
|----------|---------|------|
| Left | Back arrow | Material glyph `&#xe5c4;` (ArrowBack), 24dp font, inside a 48×48 transparent touch target. Negative 12dp left margin pulls it flush with content edge. TapGestureRecognizer calls `Shell.Current.GoToAsync("..")`. No background, border, or pill. |
| Center | Title | `{Binding Cookbook.Name}`, 22sp bold, white, flex-fills remaining space |
| Right | Edit icon | 40dp circle Border, `#1E1E3A` fill, pencil glyph `&#xe3c9;`, tap → `RenameCookbookCommand` |
| Right | Delete icon | 40dp circle Border, `#2A1E1E` fill, trash glyph `&#xe872;`, `#ff6b6b` tint, tap → `DeleteCookbookCommand` |

**Recipe count**: 13sp, `#8888AA`, left-margin 36px (aligns text start with title text). Rendered in a second row below the header row, within the same header VerticalStackLayout.

## Recipe Cards

Existing `SfCardView` structure preserved. Changes:

- Card corner radius: 10 → 12
- Thumbnail image: 110×100 → 100×90
- Card background: explicit `#14142E`
- Card margin: `4,4` → `8,4` (more vertical breathing room)
- Card padding: explicit `0`
- Kebab menu glyph `&#x22ef;` remains, bound to `EditRecipeCommand` via `RelativeSource AncestorType`

## Removed Elements

- **SfButton back button**: replaced by transparent Label + TapGestureRecognizer
- **Divider BoxView**: removed entirely (whitespace provides separation)
- **Text-based rename/delete Buttons**: replaced by icon Borders

## Design Tokens

| Token | Value |
|-------|-------|
| Page background | `#0D0D2B` (matches CookbookPage) |
| Card background | `#14142E` |
| Header padding | `20,16,16,16` |
| List padding | `12,0` margin on CollectionView |
| Title color/size | White / 22sp bold |
| Secondary text | `#8888AA` / 13sp |
| Card title | White / 15sp bold |
| Card meta | `#777` / 12sp |
| Icon button size | 40×40dp, corner radius 20 |

## Code-Behind Changes

- `OnBackClicked` signature changes from `EventHandler` to `EventHandler<TappedEventArgs>` (or kept as `EventHandler` with a lambda wrapper). The method body — `Shell.Current.GoToAsync("..")` — remains identical.
- `OnRecipeSelected` unchanged.

## ViewModel

No changes. All existing bindings (`Cookbook.Name`, `Cookbook.RecipeCount`, `Recipes`, `IsLoading`, `RenameCookbookCommand`, `DeleteCookbookCommand`, `EditRecipeCommand`, `OpenRecipeCommand`) remain.

## Files Touched

- `FridgeScan/Views/CookbookDetailPage.xaml` — primary change
- `FridgeScan/Views/CookbookDetailPage.xaml.cs` — remove `OnBackClicked` if back uses inline Shell navigation

## Risks

None. Layout-only XAML change. All bindings, commands, and code-behind navigation logic preserved.
