# ProductsPage Material 3 Expressive Redesign

**Date:** 2026-06-02
**Status:** Approved
**Author:** Sergio (via visual brainstorm)

## Overview

Redesign the inventory screen (`ProductsPage.xaml`) following Google Material 3 Expressive (2025) dark theme guidelines. Keep all existing functionality, preserve the internal row structure, and upgrade the visual presentation with larger rounded surfaces, tonal color tokens, and generous spacing.

## Scope

This spec covers **visual changes only** to `ProductsPage.xaml`. No ViewModel, model, service, or navigation changes. No new controls or third-party dependencies beyond what's already in the project.

## Files Modified

| File | Change |
|------|--------|
| `FridgeScan/Views/ProductsPage.xaml` | Full visual restyle |

## Visual Design

### Color Tokens (Dark Theme)

| Token | Value | Usage |
|-------|-------|-------|
| Page background | `#0D1023` | Content page background |
| Surface | `#171A35` | Product row cards |
| Surface Container | `#202448` | Add item card, active nav pill |
| Surface Container High | `#2A2E58` | Icon containers, expiry chip (good) |
| Surface Container (action) | `#1E1E3A` | Icon buttons (search, barcode, edit) |
| Primary (violet) | `#6750A4` | FAB accent color |
| Primary text | `#D0BCFF` | Active nav label, + icon |
| Secondary text | `#CCCCDD` | Subtitles, button labels |
| Muted text | `#8888AA` | Section item counts, metadata |
| Subtle text | `#666688` | Chevron arrow, inactive nav |
| Error surface | `#2E1E1E` | Expired badge background |
| Error text | `#FF6666` | Expired badge text |
| Warning surface | `#3A2E28` | Today badge background |
| Warning text | `#FFAA44` | Today badge text |
| Destructive surface | `#2A1E1E` | Delete icon buttons |

### Top App Bar (Large Expressive Header)

- **Title:** "Inventory" — 28sp Bold, White
- **Subtitle:** "Keep track of products and expiry dates" — 13sp, `#8888AA`
- **Actions:** Two 40dp tonal circle buttons (`#1E1E3A`) with search and barcode icons
- **Spacing:** 20dp top padding, 4dp sides, 12dp bottom

### Add Item Section (Editable Autocomplete)

- **Container:** 48dp height tonal card (`#202448`), 14dp corner radius
- **Left icon:** 32dp rounded square (`#2A2E58`) with "+" icon (Material, `#D0BCFF`)
- **Input:** `SfAutocomplete` fills remaining space — fully editable, same bindings and behavior as current implementation
- **Spacing:** 12dp horizontal padding inside card
- **Bottom margin:** 20dp before section headers

### Product List Cards

- **Height:** 48dp (reduced from 56dp)
- **Background:** `#171A35` (tonal surface)
- **Corner radius:** 14dp (up from 12dp)
- **Margin bottom:** 8dp between cards
- **Padding:** 0 horizontal (14dp on wrapper) with content filling internally

**Internal row layout (unchanged):**
```
Grid ColumnDefinitions="*,Auto,Auto,Auto"
  Column 0: Product name (Label, FontSize 14, White, LineBreakMode=TailTruncation)
  Column 1: Frozen icon (Label, Material "ac_unit", #8888AA, conditional via ShowFrozenIcon)
  Column 2: Expiry badge (Border, rounded 8dp, conditional via ShowExpiryBadge)
  Column 3: Edit button (32dp circle, #1E1E3A, edit icon)
```

### Expiry Status Badges

| State | Background | Text Color | Text |
|-------|-----------|------------|------|
| Expired | `#2E1E1E` | `#FF6666` | "Expired" |
| Today | `#3A2E28` | `#FFAA44` | "Today" |
| Good (3d or less) | `#2A2E58` | `#CCCCDD` | "Xd left" |
| Hidden (>3 days) | — | — | — |

- Badge shape: 8dp rounded pill
- Padding: 2dp vertical, 8dp horizontal
- Font: 11sp Bold

### Edit Button

- **Size:** 32dp circle (reduced from 40dp)
- **Background:** `#1E1E3A` (tonal action)
- **Icon:** Material edit pencil (`#CCCCDD`, 14–16sp)
- **Gesture:** `OnEditProductTapped` (unchanged)

### Section Headers

- **Title:** 13sp Bold, uppercase, 0.5sp letter-spacing, White
- **Count:** 11sp, `#8888AA` — "· 3 items" format
- **Spacing:** 2dp top padding, 4dp sides, 6dp bottom
- **Top margin before section:** 12dp

### Bottom Navigation Bar

- **Container:** `#171A35`, 16dp corner radius, 8dp vertical padding, 6dp horizontal
- **Active tab:** Pill-shaped indicator (`#202448`, 20dp radius), active icon `#D0BCFF`, label 11sp Bold
- **Inactive tabs:** Lower emphasis, icon/label `#8888AA`, 10sp label
- **Items:** Products (active default), Recipes, Import, Activity, Cookbooks
- **This is the existing FloatingBottomBar** control — styling update only

### Floating Action Button

- **Removed.** Barcode scanning is accessed via the header icon button only.

## What Is NOT Changing

- All `Product` model computed properties (`DaysUntilExpiry`, `ShowExpiryBadge`, `ExpiryDisplayText`, `ExpiryColor`, `ShowFrozenIcon`)
- All ViewModel bindings and commands (`GroupedProducts`, `GrocerySuggestions`, `NewItemName`, `SelectedGrocerySuggestion`, `AddItemCommand`, `BarcodeCommand`, `EditProductCommand`, `ToggleSelectCommand`, `ToggleExpandCommand`)
- `SfAutocomplete` functionality (filter behavior, suggestions, Completed event)
- `SfPullToRefresh` functionality (refreshing, pull threshold)
- `SfListView` with grouped data and sticky headers
- Section expand/collapse via `ListViewFoodCategory.IsExpanded`
- Code-behind event handlers (`SfAutocomplete_Completed`, `pullToRefresh_Refreshing`, `OnEditProductTapped`)
- `FloatingBottomBar` control (visual styling only if needed)
- Grid `RowDefinitions="Auto,*"` + `FloatingBottomBar` layout structure

## Implementation Notes

1. The page background color changes from `#0D0D2B` to `#0D1023`
2. The add item `SfAutocomplete` gets wrapped in a tonal card Border with + icon prefix
3. Product card Border gets updated radius, height, and background color — the Grid inside stays identical
4. The expiry badge Border gets updated background colors via the existing `ExpiryColor` binding — the converter/binding logic stays, but the mapped Color values change
5. The edit button Border gets reduced from 40dp to 32dp
6. The `ExpandCollapseIconConverter` stays — no changes to section header icons
7. Section header text styling changes to uppercase with letter-spacing
8. The `SfListView` outer padding increases from 10dp to 12dp

## Risk Assessment

- **Low risk** — purely visual XAML changes, no logic or data flow modifications
- The existing `ExpiryColor` computed property in `Product.cs` maps to new Color values — verify the new colors produce sufficient contrast in dark theme
- Verify that reducing row height from 56dp to 48dp doesn't clip content (current content is 14sp text with Auto columns)
