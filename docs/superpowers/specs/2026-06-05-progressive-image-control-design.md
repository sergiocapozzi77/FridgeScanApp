# ProgressiveImage Custom Control

**Date:** 2026-06-05
**Status:** Approved

## Problem

Page transitions between CookbookPage → CookbookDetailPage → SavedRecipeDetailPage are not smooth. Remote images (loaded from Appwrite/URLs) appear as blank rectangles during the 300ms fade-in entrance animation and pop in asynchronously afterward, creating a stuttering, janky navigation experience.

## Solution

A reusable `ProgressiveImage` custom control that shows a solid-colored placeholder immediately and crossfades to the real image once it finishes downloading, using an in-memory byte cache for session-level reuse.

## Architecture

### `ProgressiveImage` Control

Location: `FridgeScan/Controls/ProgressiveImage.cs`

Inherits: `ContentView`

Internal layout:

```
ProgressiveImage (ContentView)
└── Grid
    ├── Border (placeholder)
    │   └── Background = PlaceholderColor (default #161638)
    │   └── StrokeShape = RoundRectangle with CornerRadius
    ├── Image (real image, Opacity="0")
    │   └── Source set via HttpClient → FromStream
    └── Label (error fallback, emoji "🍽️")
```

### State Machine

| State | Placeholder | Image | Error Label |
|-------|-------------|-------|-------------|
| Initial (no source) | Visible (color) | Hidden, Opacity 0 | Hidden |
| Loading | Visible (color) | Hidden, Opacity 0 | Hidden |
| Success | Fades out (200ms) → Hidden | Fades in (300ms) → Visible | Hidden |
| Failed | Visible (color) | Hidden | Visible (emoji) |
| Source changed | Reset to Loading | Reset | Hidden |

### Image Loading & Caching

1. When `Source` property changes → trigger async load
2. A static `ConcurrentDictionary<string, byte[]>` cache is checked for the URL
3. Cache miss → `HttpClient.GetByteArrayAsync(url)` downloads bytes
4. On success → store in cache, set `Image.Source = ImageSource.FromStream(() => new MemoryStream(bytes))` on main thread
5. Crossfade animation: `Image.FadeTo(1, 300, CubicOut)` + `Placeholder.FadeTo(0, 200, CubicOut)` in parallel via `Task.WhenAll`
6. On failure → show error state (emoji fallback, placeholder stays visible)

The cache is unbounded per-session; image counts are small (dozens, not thousands) so eviction is unnecessary.

### Bindable Properties

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `Source` | `string` | `""` | Image URL |
| `CornerRadius` | `double` | `0` | Rounds the placeholder and clips the image |
| `Aspect` | `Aspect` | `AspectFill` | Passed to internal `Image.Aspect` |
| `PlaceholderColor` | `Color` | `#161638` | Placeholder background color |

### Error Handling

- **Download fails (404, timeout, network error)**: Placeholder stays visible, error emoji label appears centered
- **Empty/null source**: Placeholder only, no download attempted
- **Invalid URL (malformed)**: Treated as empty source

## Files Touched

| File | Change |
|------|--------|
| `Controls/ProgressiveImage.cs` | **NEW** — the custom control |
| `Views/CookbookMosaic.cs` | `CreateImage()` returns `ProgressiveImage` instead of `Image` |
| `Views/CookbookDetailPage.xaml` | Replace `<Image Source="{Binding ImageUrl}" ...>` with `<controls:ProgressiveImage Source="{Binding ImageUrl}" ...>` |
| `Views/SavedRecipeDetailPage.xaml` | Replace hero `<Image Source="{Binding Recipe.ImageUrl}" ...>` with `<controls:ProgressiveImage Source="{Binding Recipe.ImageUrl}" ...>` |

No ViewModel, service, or navigation code is changed.

## Not Changed

- `PageAnimations.FadeIn` — remains at 300ms immediate fade
- Navigation flow (Shell routes, parameters)
- Any ViewModel or data model
- Any other page

## Testing

- Build for `net9.0-android` target
- Navigate CookbookPage → CookbookDetailPage → SavedRecipeDetailPage
- Verify placeholder appears during image load (use slow network or throttling)
- Verify crossfade plays smoothly when image loads
- Verify error state on broken URL (modify image URL temporarily)
- Verify cache hit on back-navigation (image appears immediately)
