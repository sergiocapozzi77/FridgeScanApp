# Barcode Scanner Page — M3 Modernization

**Date:** 2026-06-04
**Target file:** `FridgeScan/Views/BarcodeScannerPage.xaml` (+ minor code-behind cleanup in `.xaml.cs`)

## Scope

Update `BarcodeScannerPage.xaml` to match the Material 3 design tokens and component patterns documented in CLAUDE.md. No changes to camera logic, detection behavior, or navigation flow.

## Changes

### 1. Page attributes

- Add `BackgroundColor="#0D0D2B"` to align with all other pages.
- Add `Title="Barcode Scanner"` (accessibility and Shell breadcrumb context).
- Keep `Shell.PresentationMode="ModalAnimated"`, `Unloaded="ContentPage_Unloaded"`, existing namespace imports.
- Remove `xmlns:button` (Syncfusion Buttons) import — no SfButton instances remain.

### 2. Back button — match CookbookDetailPage pattern

**Before:** `SfButton` with `Text="<"` (plain text, set in code-behind constructor).  
**After:** 48×48 transparent `Label` touch target with Material icon `&#xe5c4;` (arrow_back).

- `FontFamily="Material"`, `FontSize="24"`, `TextColor="#CCCCDD"`
- `WidthRequest="48"`, `HeightRequest="48"`, `Margin="-12,0,0,0"`
- `TapGestureRecognizer` calling the existing `BackButton_Clicked` handler
- Remove `BackButton.Text = "<"` from `BarcodeScannerPage.xaml.cs` constructor

### 3. Camera toolbar (flip + torch) — match icon button pattern

**Before:** 6-column Grid with three SfButtons (back, flip, torch) in a row.
**After:** Back button independently positioned top-left; flip and torch buttons in a `HorizontalStackLayout` top-right.

Each camera button is a 40×40 circle `Border`:
- `BackgroundColor="#1E1E3A"`, `StrokeShape="RoundRectangle 20"`, `Stroke="Transparent"`
- Material icon `Label` inside at `FontSize="18"`, `TextColor="#CCCCDD"`
- `TapGestureRecognizer` wired to existing `CameraButton_Clicked` / `TorchButton_Clicked`

Icon codepoints (Material Icons font):
- Flip camera: `&#xe412;` (flip_camera_android)
- Torch: `&#xe42b;` (flashlight_on)

### 4. Viewfinder scrim overlay

A dark semi-transparent overlay (`rgba(0,0,0,0.55)`) covering the camera feed except for a centered rectangular cutout where the barcode is framed.

**Implementation:** 4 BoxView bars in a 3×3 Grid:

```
┌──────────────────────────────┐
│         TOP BAR              │  ← BoxView, height auto / fixed
├──────────┬──────────┬────────┤
│ LEFT BAR │ CUTOUT   │ RIGHT  │  ← side bars fixed width
│ (fixed)  │ (transp.)│ (fixed)│
├──────────┴──────────┴────────┤
│        BOTTOM BAR            │  ← BoxView, height auto / fixed
└──────────────────────────────┘
```

- All bars: `BackgroundColor="#8C000000"` (hex: 55% opacity black)
- Center cell: empty — no element, so it's naturally transparent and shows camera feed
- Entire overlay `InputTransparent="True"` so camera detection and tap-to-focus pass through
- Places above `CameraView` and `GraphicsView` in z-order

**Corner markers:** Four small Borders at the inner edges of the cutout, each 24×24dp, with a 3px white stroke (`#B3FFFFFF`) on the two outward-facing sides (top+left, top+right, bottom+left, bottom+right). Sits above the scrim in z-order but below the toolbar. Provides visual framing for the scan target area.

### 5. Product panel — match M3 card pattern

**Before:** `BackgroundColor="#CCFFFFFF"` (semitransparent white), black text, SfButton for Add/Cancel.  
**After:** M3 tonal card anchored at bottom:

| Element | Value |
|---------|-------|
| Card background | `#14142E` |
| Corner radius | 16 |
| Product name | 15sp Bold, White |
| Add button | Pill Border, `#1E1E3A` bg, `#CCCCDD` text, bold 13sp |
| Cancel button | Pill Border, `#2A1E1E` bg, `#ff6b6b` text, bold 13sp |
| Margins | 16dp sides, 20dp bottom |

## Z-order (bottom → top)

1. `CameraView` — camera feed (fills entire background)
2. `GraphicsView` — barcode detection bounding boxes (drawn above camera)
3. Scrim overlay (4 BoxView bars, `InputTransparent="True"`) — darkens periphery, pass-through for touch
4. Corner markers — 4 Border elements at cutout edges, above scrim
5. Back button + flip/torch toolbar — interactive controls, above all overlays
6. Product panel — scan result overlay (initially hidden, appears on detection)

## Code-behind changes

- **`BarcodeScannerPage.xaml.cs`** — remove `BackButton.Text = "<"` from constructor (line 19). No other logic changes.

## Files modified

- `FridgeScan/Views/BarcodeScannerPage.xaml`
- `FridgeScan/Views/BarcodeScannerPage.xaml.cs` (1 line removed)

## Out of scope

- No camera logic or detection flow changes
- No viewmodel or service layer changes
- No navigation changes
