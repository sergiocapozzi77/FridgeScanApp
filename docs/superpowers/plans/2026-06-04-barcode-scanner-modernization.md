# Barcode Scanner Page — M3 Modernization Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Update `BarcodeScannerPage.xaml` to match M3 design tokens and component patterns from CLAUDE.md, with a viewfinder scrim overlay.

**Architecture:** Single-page XAML rewrite + one-line code-behind cleanup. No logic changes, no new files, no viewmodel changes.

**Tech Stack:** .NET MAUI XAML, Material Icons font, BoxView scrim overlay

---

### Task 1: Rewrite BarcodeScannerPage.xaml with M3 modernization

**Files:**
- Modify: `FridgeScan/Views/BarcodeScannerPage.xaml` (full rewrite)

- [ ] **Step 1: Replace the entire file with the M3-modernized XAML**

The new file uses `Write` to replace all content. Key sections:

**Page attributes:** Add `BackgroundColor="#0D0D2B"`, `Title="Barcode Scanner"`, remove `xmlns:button`.

**Back button:** 48×48 transparent Label with Material icon `&#xe5c4;` (arrow_back). Positioned top-left.

**Camera toolbar:** Two 40×40 circle Border buttons (flip + torch) in a HorizontalStackLayout top-right.

**Scrim overlay:** 3×3 Grid with 4 BoxView bars (`#8C000000`, ~55% opacity) creating a centered transparent cutout. The center cell holds corner markers.

**Corner markers:** 8 BoxView elements (2 per corner: a 3px-wide line and a 3px-tall line forming an L-shape) in `#B3FFFFFF`.

**Product panel:** M3 card (`#14142E`, corner 16), white product name text, pill-style Add (`#1E1E3A`) and Cancel (`#2A1E1E`) buttons.

**Z-order (within the single Grid):** CameraView (bottom) → GraphicsView → scrim overlay → corner markers → toolbar → ProductPanel (top).

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    x:Class="FridgeScan.Views.BarcodeScannerPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:barcode="clr-namespace:BarcodeScanning;assembly=BarcodeScanning.Native.Maui"
    BackgroundColor="#0D0D2B"
    Title="Barcode Scanner"
    Shell.PresentationMode="ModalAnimated"
    Unloaded="ContentPage_Unloaded">

    <Grid>
        <!-- Layer 1: Camera feed -->
        <barcode:CameraView
            x:Name="Barcode"
            AimMode="True"
            BarcodeSymbologies="All"
            CaptureQuality="High"
            OnDetectionFinished="CameraView_OnDetectionFinished"
            TapToFocusEnabled="True" />

        <!-- Layer 2: Barcode detection bounding boxes -->
        <GraphicsView
            x:Name="Graphics"
            AbsoluteLayout.LayoutBounds="0,0,1,1"
            AbsoluteLayout.LayoutFlags="All"
            InputTransparent="True" />

        <!-- Layer 3: Scrim overlay with center cutout -->
        <Grid InputTransparent="True">
            <Grid RowDefinitions="*,2*,*" ColumnDefinitions="*,3*,*">
                <!-- Top bar -->
                <BoxView Grid.ColumnSpan="3" BackgroundColor="#8C000000" />
                <!-- Bottom bar -->
                <BoxView Grid.Row="2" Grid.ColumnSpan="3" BackgroundColor="#8C000000" />
                <!-- Left bar -->
                <BoxView Grid.Row="1" BackgroundColor="#8C000000" />
                <!-- Right bar -->
                <BoxView Grid.Row="1" Grid.Column="2" BackgroundColor="#8C000000" />

                <!-- Center cell: transparent cutout + corner markers -->
                <Grid Grid.Row="1" Grid.Column="1">
                    <!-- Top-left L -->
                    <BoxView WidthRequest="24" HeightRequest="3" BackgroundColor="#B3FFFFFF" HorizontalOptions="Start" VerticalOptions="Start" />
                    <BoxView WidthRequest="3" HeightRequest="24" BackgroundColor="#B3FFFFFF" HorizontalOptions="Start" VerticalOptions="Start" />
                    <!-- Top-right L -->
                    <BoxView WidthRequest="24" HeightRequest="3" BackgroundColor="#B3FFFFFF" HorizontalOptions="End" VerticalOptions="Start" />
                    <BoxView WidthRequest="3" HeightRequest="24" BackgroundColor="#B3FFFFFF" HorizontalOptions="End" VerticalOptions="Start" />
                    <!-- Bottom-left L -->
                    <BoxView WidthRequest="24" HeightRequest="3" BackgroundColor="#B3FFFFFF" HorizontalOptions="Start" VerticalOptions="End" />
                    <BoxView WidthRequest="3" HeightRequest="24" BackgroundColor="#B3FFFFFF" HorizontalOptions="Start" VerticalOptions="End" />
                    <!-- Bottom-right L -->
                    <BoxView WidthRequest="24" HeightRequest="3" BackgroundColor="#B3FFFFFF" HorizontalOptions="End" VerticalOptions="End" />
                    <BoxView WidthRequest="3" HeightRequest="24" BackgroundColor="#B3FFFFFF" HorizontalOptions="End" VerticalOptions="End" />
                </Grid>
            </Grid>
        </Grid>

        <!-- Layer 4: Back button — Material icon, 48dp transparent touch target -->
        <Label Text="&#xe5c4;"
               FontFamily="Material"
               FontSize="24"
               TextColor="#CCCCDD"
               WidthRequest="48"
               HeightRequest="48"
               Margin="-12,0,0,0"
               HorizontalTextAlignment="Center"
               VerticalTextAlignment="Center"
               VerticalOptions="Start"
               HorizontalOptions="Start">
            <Label.GestureRecognizers>
                <TapGestureRecognizer Clicked="BackButton_Clicked" />
            </Label.GestureRecognizers>
        </Label>

        <!-- Layer 4: Camera toolbar (flip + torch) — 40dp circle Borders -->
        <HorizontalStackLayout HorizontalOptions="End"
                               VerticalOptions="Start"
                               Spacing="8"
                               Margin="0,12,16,0">
            <Border BackgroundColor="#1E1E3A"
                    StrokeShape="RoundRectangle 20"
                    Stroke="Transparent"
                    WidthRequest="40"
                    HeightRequest="40">
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Clicked="CameraButton_Clicked" />
                </Border.GestureRecognizers>
                <Label Text="&#xe412;"
                       FontFamily="Material"
                       FontSize="18"
                       TextColor="#CCCCDD"
                       HorizontalOptions="Center"
                       VerticalOptions="Center" />
            </Border>
            <Border BackgroundColor="#1E1E3A"
                    StrokeShape="RoundRectangle 20"
                    Stroke="Transparent"
                    WidthRequest="40"
                    HeightRequest="40">
                <Border.GestureRecognizers>
                    <TapGestureRecognizer Clicked="TorchButton_Clicked" />
                </Border.GestureRecognizers>
                <Label Text="&#xe42b;"
                       FontFamily="Material"
                       FontSize="18"
                       TextColor="#CCCCDD"
                       HorizontalOptions="Center"
                       VerticalOptions="Center" />
            </Border>
        </HorizontalStackLayout>

        <!-- Layer 5: Product panel — M3 card, appears on barcode detection -->
        <Border x:Name="ProductPanel"
                Margin="16,0,16,20"
                Padding="12"
                BackgroundColor="#14142E"
                HeightRequest="160"
                HorizontalOptions="Fill"
                VerticalOptions="End"
                IsVisible="False"
                Opacity="0">
            <Border.StrokeShape>
                <RoundRectangle CornerRadius="16" />
            </Border.StrokeShape>
            <HorizontalStackLayout Spacing="15">
                <Image x:Name="ProductImage"
                       Aspect="AspectFit"
                       HeightRequest="80"
                       WidthRequest="80" />
                <VerticalStackLayout Spacing="10"
                                     VerticalOptions="Center">
                    <Label x:Name="ProductName"
                           FontSize="15"
                           FontAttributes="Bold"
                           TextColor="White"
                           MaxLines="2"
                           LineBreakMode="WordWrap" />
                    <HorizontalStackLayout Spacing="10">
                        <Border BackgroundColor="#1E1E3A"
                                StrokeShape="RoundRectangle 16"
                                Stroke="Transparent"
                                HeightRequest="32"
                                Padding="16,0">
                            <Border.GestureRecognizers>
                                <TapGestureRecognizer Clicked="AddButton_Clicked" />
                            </Border.GestureRecognizers>
                            <Label Text="Add"
                                   FontSize="13"
                                   FontAttributes="Bold"
                                   TextColor="#CCCCDD"
                                   VerticalOptions="Center"
                                   HorizontalOptions="Center" />
                        </Border>
                        <Border BackgroundColor="#2A1E1E"
                                StrokeShape="RoundRectangle 16"
                                Stroke="Transparent"
                                HeightRequest="32"
                                Padding="16,0">
                            <Border.GestureRecognizers>
                                <TapGestureRecognizer Clicked="CancelButton_Clicked" />
                            </Border.GestureRecognizers>
                            <Label Text="Cancel"
                                   FontSize="13"
                                   FontAttributes="Bold"
                                   TextColor="#ff6b6b"
                                   VerticalOptions="Center"
                                   HorizontalOptions="Center" />
                        </Border>
                    </HorizontalStackLayout>
                </VerticalStackLayout>
            </HorizontalStackLayout>
        </Border>
    </Grid>
</ContentPage>
```

---

### Task 2: Clean up code-behind

**Files:**
- Modify: `FridgeScan/Views/BarcodeScannerPage.xaml.cs:19`

- [ ] **Step 1: Remove `BackButton.Text = "<"` from constructor**

The back button is now a Material icon Label in XAML, so the manual text assignment in code-behind is no longer needed.

Delete line 19 (`BackButton.Text = "<";`) from the constructor:

```csharp
public BarcodeScannerPage()
{
    InitializeComponent();

}
```

---

### Task 3: Build and verify

- [ ] **Step 1: Build for Android**

Run: `dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android`

Expected: Build succeeds with no errors or warnings related to the modified files. The removed `xmlns:button` import and `BackButton.Text` line should not cause any issues since all SfButton references are gone.

---

## Spec coverage checklist

| Spec section | Covered by |
|---|---|
| 1. Page attributes (BackgroundColor, Title, remove xmlns:button) | Task 1 — XAML ContentPage attributes |
| 2. Back button (Material icon, 48dp, transparent) | Task 1 — Label with &#xe5c4; |
| 3. Camera toolbar (40dp circle Borders) | Task 1 — HorizontalStackLayout with 2 Borders |
| 4. Viewfinder scrim (4 BoxView bars, 3×3 Grid, #8C000000) | Task 1 — Scrim Grid with BoxViews |
| 5. Corner markers (24dp, white L-shapes) | Task 1 — 8 BoxViews in center cell |
| 6. Product panel (#14142E, pill buttons) | Task 1 — Border with Add/Cancel pills |
| 7. Code-behind cleanup (remove BackButton.Text) | Task 2 — Delete line 19 |
