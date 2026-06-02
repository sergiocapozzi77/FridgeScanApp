# TabBar Redesign — A3 Floating Shelf

**Date:** 2026-06-02
**Status:** Approved
**Theme:** Material 3 Expressive Dark

## Overview

Replace the default Shell TabBar with a custom floating bottom navigation bar ("Floating Shelf") that follows the M3 design tokens established in the app.

## Architecture

- Shell still handles page routing and navigation — only the visual tab bar is replaced.
- `Shell.TabBarIsVisible="False"` hides the native Shell TabBar.
- A reusable `FloatingBottomBar` ContentView sits at the bottom of each page via a two-row Grid layout `(RowDefinitions="*,Auto")`.
- Tab switching calls `Shell.Current.GoToAsync("///routeName")`.

## Visual Spec

### Container
- Background: `#14142E` (card surface token)
- Corner radius: 16dp all around (`RoundRectangle 16`)
- Horizontal margin: 12dp each side
- Bottom margin: 8dp (above safe area)
- Shadow: `Brush=#000000, Opacity=0.3, Offset=0,4, Radius=16`
- Padding inside bar: 8dp horizontal, 6dp vertical

### Tabs (5 total)
| # | Tab | Route | Icon |
|---|-----|-------|------|
| 1 | Products | `///products` | `&#xe85d;` (inventory) |
| 2 | Recipe | `///recipe` | `&#xf357;` (cookbook) |
| 3 | Cookbooks | `///cookbook` | `&#xe86d;` (book) |
| 4 | Import | `///import` | `icon_import.png` |
| 5 | Activities | `///activities` | `&#xe889;` (notifications) |

### Tab States

**Active tab:**
- Pill background: `#1E1E3A` (action surface), corner radius 14dp
- Icon: White, 20sp, Material font
- Label: White, 9sp, regular weight
- Touch target: 56×44dp minimum

**Inactive tab:**
- No background
- Icon: `#8888AA` (muted text token), 20sp
- Label: `#8888AA`, 9sp

### Tab Layout
- Tabs distributed evenly via `HorizontalStackLayout` with `HorizontalOptions="FillAndExpand"`
- Each tab: vertical stack of icon (20sp) + label (9sp), centered, 1sp gap between
- 5 tabs, equal visual weight

## Component Design

### FloatingBottomBar (ContentView)
- Bindable property `ActiveRoute` or auto-detection from `Shell.Current.CurrentState.Location`
- Each tab is a `Border` with a `TapGestureRecognizer`
- Built-in `BottomTab` data model: `{Glyph, Label, Route}` — used by an `ItemsSource` + `BindableLayout`

### Navigation State
The bar listens to Shell route changes to update the active tab. Implementation: observe `Shell.Current.Navigated` (or `Navigating`) event and compare the URI to each tab's route prefix. Highlight the matching tab; fall back to Products on app start.

## Platform Notes

### Android / iOS
- Standard Shell TabBar hidden via `Shell.TabBarIsVisible="False"`
- Safe area at bottom handled by the margin system naturally — the bar sits above the home indicator
- Content pages wrap their existing content in a Grid with the bar appended

### Windows
- Floating bar at window bottom. On wide screens, consider capping the bar width (e.g., 600dp max) and centering it horizontally rather than stretching edge-to-edge with margins

## File Changes

| File | Change |
|------|--------|
| `AppShell.xaml` | Hide TabBar, keep ShellContent definitions for route registration |
| `AppShell.xaml.cs` | Register routes (already done) |
| `FridgeScan/Controls/FloatingBottomBar.xaml` | **New** — Floating Shelf bar |
| `FridgeScan/Controls/FloatingBottomBar.xaml.cs` | **New** — code-behind + route listener |
| `FridgeScan/Services/NavigationService.cs` | Optional — if route-to-tab mapping extracted |
| `FridgeScan/Views/ProductsPage.xaml` | Wrap in Grid with row for FloatingBottomBar |
| `FridgeScan/Views/RecipePage.xaml` | Same |
| `FridgeScan/Views/ImportPage.xaml` | Same |
| `FridgeScan/Views/ActivitiesPage.xaml` | Same |
| `FridgeScan/Views/CookbookPage.xaml` | Same |

## Out of Scope
- Tab reordering
- Badge counts on tabs
- Animated tab transitions
- Haptic feedback on tab press
