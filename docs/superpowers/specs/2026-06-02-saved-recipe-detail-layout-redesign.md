# SavedRecipeDetailPage — Ingredient & Method Layout Redesign

**Date:** 2026-06-02
**Status:** Approved for implementation
**Layout choice:** Classic Card Sections (Option 1)
**Ingredient arrangement:** Vertical checklist

---

## 1. Summary

Redesign the ingredients and method sections of `SavedRecipeDetailPage` to match the project's Material 3 design standards. Ingredients become a tappable vertical checklist with strikethrough; method steps get numbered badges.

---

## 2. Changes

### 2.1 Data model

**New class: `IngredientItem`** (in `Models/IngredientItem.cs`)

```csharp
public partial class IngredientItem : ObservableObject
{
    [ObservableProperty]
    private string name;

    [ObservableProperty]
    [AlsoNotifyChangeFor(nameof(TextColor), nameof(Opacity))]
    private bool isChecked;

    public TextDecorations TextDecorations
        => IsChecked ? TextDecorations.Strikethrough : TextDecorations.None;

    public double Opacity
        => IsChecked ? 0.5 : 1.0;

    public Color TextColor
        => IsChecked ? Color.FromArgb("#8888AA") : Color.FromArgb("#CCCCDD");
}
```

**No changes to `SavedRecipe`.** The persisted model stays `List<string> Ingredients`. `IngredientItem` is a view-level wrapper created on load and not persisted.

### 2.2 ViewModel changes (`SavedRecipeDetailViewModel`)

Add:
- `ObservableCollection<IngredientItem> IngredientItems` — populated from `Recipe.Ingredients` after recipe loads
- `[RelayCommand] void ToggleIngredient(IngredientItem item)` — toggles `IsChecked`

```csharp
public ObservableCollection<IngredientItem> IngredientItems { get; } = new();

[RelayCommand]
private void ToggleIngredient(IngredientItem item)
{
    item.IsChecked = !item.IsChecked;
}
```

Call `BuildIngredientItems()` at the end of both `LoadRecipeAsync()` and `LoadRecipeDetails()` (after `Recipe` is assigned).

**Step numbering:** Use a `MethodStep` wrapper class — same pattern as `IngredientItem`. Each step gets its index pre-computed when building the list.

### 2.3 New model: `MethodStep`

```csharp
// Models/MethodStep.cs (sealed, not ObservableObject — no interaction needed)
public class MethodStep
{
    public int Number { get; set; }
    public string Text { get; set; } = string.Empty;
}
```

**ViewModel** builds `MethodSteps` from `Recipe.MethodSteps`:

```csharp
public ObservableCollection<MethodStep> MethodSteps { get; } = new();

private void BuildMethodSteps()
{
    MethodSteps.Clear();
    for (int i = 0; i < Recipe.MethodSteps.Count; i++)
        MethodSteps.Add(new MethodStep { Number = i + 1, Text = Recipe.MethodSteps[i] });
}
```

### 2.4 View layout (`SavedRecipeDetailPage.xaml`)

**Replace these elements** inside the ScrollView:

```xml
<!-- OLD: Frame-based ingredients -->
<Frame BackgroundColor="#252525" BorderColor="Transparent" CornerRadius="10" ...>
    <CollectionView ... />
</Frame>

<!-- OLD: Frame-based method -->
<Frame BackgroundColor="#252525" BorderColor="Transparent" CornerRadius="10" ...>
    <CollectionView ... />
</Frame>
```

**With:**

#### Ingredients card

```xml
<sfCards:SfCardView CornerRadius="16" Padding="0" BackgroundColor="#14142E"
                    xmlns:sfCards="clr-namespace:Syncfusion.Maui.Cards;assembly=Syncfusion.Maui.Cards">
    <VerticalStackLayout Spacing="0" Padding="16,12">
        <Label Text="Ingredients" FontSize="15" FontAttributes="Bold"
               TextColor="White" Margin="0,0,0,8" />
        <CollectionView ItemsSource="{Binding IngredientItems}" SelectionMode="None">
            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="models:IngredientItem">
                    <Border Padding="0,6" BackgroundColor="Transparent">
                        <Border.GestureRecognizers>
                            <TapGestureRecognizer
                                Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodel:SavedRecipeDetailViewModel}}, Path=ToggleIngredientCommand}"
                                CommandParameter="{Binding .}" />
                        </Border.GestureRecognizers>
                        <Grid ColumnDefinitions="Auto,*" ColumnSpacing="12"
                              Opacity="{Binding Opacity}">
                            <!-- Circle indicator -->
                            <Border WidthRequest="22" HeightRequest="22"
                                    StrokeShape="RoundRectangle 11"
                                    Stroke="#CCCCDD" StrokeThickness="1.5"
                                    BackgroundColor="Transparent">
                                <Label Text="✓" FontSize="12" TextColor="#CCCCDD"
                                       HorizontalOptions="Center" VerticalOptions="Center"
                                       IsVisible="{Binding IsChecked}" />
                            </Border>
                            <!-- Ingredient name -->
                            <Label Grid.Column="1" Text="{Binding Name}"
                                   FontSize="14" TextColor="{Binding TextColor}"
                                   TextDecorations="{Binding TextDecorations}"
                                   VerticalOptions="Center" />
                        </Grid>
                    </Border>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>
    </VerticalStackLayout>
</sfCards:SfCardView>
```

#### Method card

```xml
<sfCards:SfCardView CornerRadius="16" Padding="0" BackgroundColor="#14142E">
    <VerticalStackLayout Spacing="0" Padding="16,12">
        <Label Text="Method" FontSize="15" FontAttributes="Bold"
               TextColor="White" Margin="0,0,0,8" />
        <CollectionView ItemsSource="{Binding MethodSteps}" SelectionMode="None">
            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="models:MethodStep">
                    <Grid Padding="0,8" ColumnDefinitions="Auto,*" ColumnSpacing="12">
                        <!-- Step number badge -->
                        <Border WidthRequest="24" HeightRequest="24"
                                StrokeShape="RoundRectangle 12"
                                Stroke="Transparent"
                                BackgroundColor="#1E1E3A">
                            <Label Text="{Binding Number}"
                                   FontSize="12" FontAttributes="Bold"
                                   TextColor="#CCCCDD"
                                   HorizontalOptions="Center" VerticalOptions="Center" />
                        </Border>
                        <!-- Step text -->
                        <Label Grid.Column="1" Text="{Binding Text}"
                               FontSize="14" TextColor="#CCCCDD"
                               LineBreakMode="WordWrap" VerticalOptions="Center" />
                    </Grid>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>
    </VerticalStackLayout>
</sfCards:SfCardView>
```

### 2.5 Spacing between sections

The previous `Spacing="12"` on the parent `VerticalStackLayout` (which wraps the content inside the ScrollView) already provides the vertical gap. No extra margin needed between cards.

---

## 3. Visual states

| Element | Unchecked | Checked |
|---------|-----------|---------|
| Circle indicator | 22×22 hollow ring, `BorderColor #CCCCDD` | Filled `#CCCCDD`, ✓ visible inside |
| Ingredient text | `#CCCCDD`, normal weight | `#8888AA`, strikethrough, 0.5 opacity |
| Row opacity | 1.0 | 0.5 |

Method step number badges are always visible (`#1E1E3A` bg, `#CCCCDD` text) — no interaction.

---

## 4. Edge cases

- **Many ingredients** — CollectionView inside ScrollView: leave CollectionView height unconstrained (VerticalStackLayout parent handles overflow scrolling).
- **Very long ingredient name** — wraps to next line. Circle alignment is center (vertical) to handle multi-line gracefully.
- **Empty list** — card title still renders; CollectionView is empty. Same as current behavior.
- **Checked state persistence** — NOT persisted. Ephemeral per-session. Page navigation resets all items to unchecked.
- **Null/empty step** — step badge still shows number with empty text. Works as today.

---

## 5. Files to touch

| File | Change |
|------|--------|
| `Models/IngredientItem.cs` | **New** — `ObservableObject` with `Name`, `IsChecked`, computed `TextDecorations`, `Opacity`, `TextColor` |
| `Models/MethodStep.cs` | **New** — simple wrapper with `Number` (int) and `Text` (string) |
| `ViewModels/SavedRecipeDetailViewModel.cs` | Add `IngredientItems`, `MethodSteps`, `ToggleIngredientCommand`, `BuildIngredientItems()`, `BuildMethodSteps()` |
| `Views/SavedRecipeDetailPage.xaml` | Replace Frame-based sections with SfCardView sections |

---

## 6. Non-goals

- No change to the persisted `SavedRecipe` model
- No change to the recipe image, header, metadata chips, description, or nutrition sections
- No persistence of checked ingredient state
- No changes to other pages (RecipePreviewPage, etc.)
- No animation on strikethrough transition
