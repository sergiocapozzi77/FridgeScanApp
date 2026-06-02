# SavedRecipeDetailPage Layout Redesign — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Frame-based ingredients and method sections on SavedRecipeDetailPage with Material 3 SfCardView sections, add tap-to-strikethrough for ingredients, and number method steps.

**Architecture:** Two new view-level models (`IngredientItem`, `MethodStep`) wrap the existing `SavedRecipe` string lists. ViewModel gains observable collections and a toggle command. View swaps Frames for SfCardView.

**Tech Stack:** .NET MAUI, Syncfusion MAUI Cards (SfCardView), CommunityToolkit.Mvvm source generators

---

### Task 1: Create `IngredientItem` model

**Files:**
- Create: `FridgeScan/Models/IngredientItem.cs`

- [ ] **Step 1: Write `IngredientItem.cs`**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace FridgeScan.Models;

public partial class IngredientItem : ObservableObject
{
    public IngredientItem(string name)
    {
        Name = name;
    }

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

- [ ] **Step 2: Build check**

Run: `dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android --no-restore`
Expected: Build succeeds (warnings OK).

Note: If the build fails, check that `CommunityToolkit.Mvvm` NuGet is referenced in the project (it is — used by `Product.cs` and all ViewModels).

- [ ] **Step 3: Commit**

```bash
git add FridgeScan/Models/IngredientItem.cs
git commit -m "feat: add IngredientItem view model for strikethrough checklist"
```

---

### Task 2: Create `MethodStep` model

**Files:**
- Create: `FridgeScan/Models/MethodStep.cs`

- [ ] **Step 1: Write `MethodStep.cs`**

```csharp
namespace FridgeScan.Models;

public class MethodStep
{
    public int Number { get; set; }
    public string Text { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Build check**

Run: `dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android --no-restore`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add FridgeScan/Models/MethodStep.cs
git commit -m "feat: add MethodStep model for numbered method steps"
```

---

### Task 3: Update ViewModel — add collections, commands, and build methods

**Files:**
- Modify: `FridgeScan/ViewModels/SavedRecipeDetailViewModel.cs`

- [ ] **Step 1: Add ObservableCollections and ToggleIngredientCommand**

Add these members after the existing `MetadataChips` collection (around line 34):

```csharp
public ObservableCollection<IngredientItem> IngredientItems { get; } = new();
public ObservableCollection<MethodStep> MethodSteps { get; } = new();

[RelayCommand]
private void ToggleIngredient(IngredientItem item)
{
    item.IsChecked = !item.IsChecked;
}
```

- [ ] **Step 2: Add `BuildIngredientItems()` and `BuildMethodSteps()` helper methods**

Add these methods after `DeleteRecipeCommand` (before the closing brace of the class, around line 233):

```csharp
private void BuildIngredientItems()
{
    IngredientItems.Clear();
    if (Recipe?.Ingredients == null) return;
    foreach (var ing in Recipe.Ingredients)
        IngredientItems.Add(new IngredientItem(ing));
}

private void BuildMethodSteps()
{
    MethodSteps.Clear();
    if (Recipe?.MethodSteps == null) return;
    for (int i = 0; i < Recipe.MethodSteps.Count; i++)
        MethodSteps.Add(new MethodStep { Number = i + 1, Text = Recipe.MethodSteps[i] });
}
```

- [ ] **Step 3: Wire up build calls in `LoadRecipeAsync`**

At the end of `LoadRecipeAsync`, after `NotifyVisibilityChanged()` (line 77), add:

```csharp
BuildIngredientItems();
BuildMethodSteps();
```

- [ ] **Step 4: Wire up build calls in `LoadRecipeDetails`**

At the end of `LoadRecipeDetails`, after `NotifyVisibilityChanged()` (line 120), add:

```csharp
BuildIngredientItems();
BuildMethodSteps();
```

- [ ] **Step 5: Build check**

Run: `dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android --no-restore`
Expected: Build succeeds.

- [ ] **Step 6: Commit**

```bash
git add FridgeScan/ViewModels/SavedRecipeDetailViewModel.cs
git commit -m "feat: add ingredient checklist and method step collections to VM"
```

---

### Task 4: Replace Frame-based sections with SfCardView in XAML

**Files:**
- Modify: `FridgeScan/Views/SavedRecipeDetailPage.xaml`

- [ ] **Step 1: Add namespace declarations to the ContentPage root**

Change the ContentPage opening tag from:

```xml
<ContentPage
    x:Class="FridgeScan.Views.SavedRecipeDetailPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:sfBusy="clr-namespace:Syncfusion.Maui.Core;assembly=Syncfusion.Maui.Core"
    BackgroundColor="#0D0D2B"
    Title="{Binding Recipe.Name}">
```

To:

```xml
<ContentPage
    x:Class="FridgeScan.Views.SavedRecipeDetailPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:sfBusy="clr-namespace:Syncfusion.Maui.Core;assembly=Syncfusion.Maui.Core"
    xmlns:sfCards="clr-namespace:Syncfusion.Maui.Cards;assembly=Syncfusion.Maui.Cards"
    xmlns:models="clr-namespace:FridgeScan.Models"
    xmlns:viewmodel="clr-namespace:FridgeScan.ViewModels"
    BackgroundColor="#0D0D2B"
    Title="{Binding Recipe.Name}">
```

- [ ] **Step 2: Replace the ingredients section**

Find this block (roughly lines 111-132 in the current file):

```xml
                    <!-- Ingredients -->
                    <Label Text="Ingredients"
                           FontSize="16"
                           FontAttributes="Bold"
                           TextColor="White" />
                    <Frame BackgroundColor="#252525"
                           BorderColor="Transparent"
                           CornerRadius="10"
                           Padding="0"
                           HasShadow="False">
                        <CollectionView ItemsSource="{Binding Recipe.Ingredients}"
                                        SelectionMode="None">
                            <CollectionView.ItemTemplate>
                                <DataTemplate x:DataType="x:String">
                                    <Label Text="{Binding .}"
                                           FontSize="14"
                                           Padding="14,6"
                                           TextColor="#CCCCDD" />
                                </DataTemplate>
                            </CollectionView.ItemTemplate>
                        </CollectionView>
                    </Frame>
```

Replace with:

```xml
                    <!-- Ingredients card -->
                    <sfCards:SfCardView CornerRadius="16" Padding="0"
                                        BackgroundColor="#14142E">
                        <VerticalStackLayout Spacing="0" Padding="16,12">
                            <Label Text="Ingredients"
                                   FontSize="15" FontAttributes="Bold"
                                   TextColor="White" Margin="0,0,0,8" />
                            <CollectionView ItemsSource="{Binding IngredientItems}"
                                            SelectionMode="None">
                                <CollectionView.ItemTemplate>
                                    <DataTemplate x:DataType="models:IngredientItem">
                                        <Border Padding="0,6" BackgroundColor="Transparent">
                                            <Border.GestureRecognizers>
                                                <TapGestureRecognizer
                                                    Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodel:SavedRecipeDetailViewModel}}, Path=ToggleIngredientCommand}"
                                                    CommandParameter="{Binding .}" />
                                            </Border.GestureRecognizers>
                                            <Grid ColumnDefinitions="Auto,*"
                                                  ColumnSpacing="12"
                                                  Opacity="{Binding Opacity}">
                                                <!-- Circle indicator -->
                                                <Border WidthRequest="22" HeightRequest="22"
                                                        StrokeShape="RoundRectangle 11"
                                                        Stroke="#CCCCDD" StrokeThickness="1.5"
                                                        BackgroundColor="Transparent">
                                                    <Label Text="&#10003;" FontSize="12"
                                                           TextColor="#CCCCDD"
                                                           HorizontalOptions="Center"
                                                           VerticalOptions="Center"
                                                           IsVisible="{Binding IsChecked}" />
                                                </Border>
                                                <!-- Ingredient name -->
                                                <Label Grid.Column="1" Text="{Binding Name}"
                                                       FontSize="14"
                                                       TextColor="{Binding TextColor}"
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

- [ ] **Step 3: Replace the method section**

Find this block (roughly lines 134-160):

```xml
                    <!-- Method -->
                    <Label Text="Method"
                           FontSize="16"
                           FontAttributes="Bold"
                           TextColor="White" />
                    <Frame BackgroundColor="#252525"
                           BorderColor="Transparent"
                           CornerRadius="10"
                           Padding="0"
                           HasShadow="False">
                        <CollectionView ItemsSource="{Binding Recipe.MethodSteps}"
                                        SelectionMode="None">
                            <CollectionView.ItemTemplate>
                                <DataTemplate x:DataType="x:String">
                                    <Grid Padding="14,8"
                                          ColumnDefinitions="Auto,*"
                                          ColumnSpacing="10">
                                        <Label Grid.Column="1"
                                               Text="{Binding .}"
                                               FontSize="14"
                                               LineBreakMode="WordWrap"
                                               TextColor="#CCCCDD" />
                                    </Grid>
                                </DataTemplate>
                            </CollectionView.ItemTemplate>
                        </CollectionView>
                    </Frame>
```

Replace with:

```xml
                    <!-- Method card -->
                    <sfCards:SfCardView CornerRadius="16" Padding="0"
                                        BackgroundColor="#14142E">
                        <VerticalStackLayout Spacing="0" Padding="16,12">
                            <Label Text="Method"
                                   FontSize="15" FontAttributes="Bold"
                                   TextColor="White" Margin="0,0,0,8" />
                            <CollectionView ItemsSource="{Binding MethodSteps}"
                                            SelectionMode="None">
                                <CollectionView.ItemTemplate>
                                    <DataTemplate x:DataType="models:MethodStep">
                                        <Grid Padding="0,8"
                                              ColumnDefinitions="Auto,*"
                                              ColumnSpacing="12">
                                            <!-- Step number badge -->
                                            <Border WidthRequest="24" HeightRequest="24"
                                                    StrokeShape="RoundRectangle 12"
                                                    Stroke="Transparent"
                                                    BackgroundColor="#1E1E3A">
                                                <Label Text="{Binding Number}"
                                                       FontSize="12" FontAttributes="Bold"
                                                       TextColor="#CCCCDD"
                                                       HorizontalOptions="Center"
                                                       VerticalOptions="Center" />
                                            </Border>
                                            <!-- Step text -->
                                            <Label Grid.Column="1" Text="{Binding Text}"
                                                   FontSize="14" TextColor="#CCCCDD"
                                                   LineBreakMode="WordWrap"
                                                   VerticalOptions="Center" />
                                        </Grid>
                                    </DataTemplate>
                                </CollectionView.ItemTemplate>
                            </CollectionView>
                        </VerticalStackLayout>
                    </sfCards:SfCardView>
```

- [ ] **Step 4: Build check**

Run: `dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android --no-restore`
Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add FridgeScan/Views/SavedRecipeDetailPage.xaml
git commit -m "feat: replace Frame sections with SfCardView in recipe detail"
```

---

### Task 5: Final verification

- [ ] **Step 1: Full build**

Run: `dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android`
Expected: Build succeeds with no errors.

- [ ] **Step 2: Review all changes**

```bash
git log --oneline -5
git diff HEAD~4..HEAD --stat
```

Expected: 4 commits, 4 files changed (2 new, 2 modified).

- [ ] **Step 3: Visual sanity check** (run on device/emulator)

```bash
dotnet run --project FridgeScan/FridgeScan.csproj -f net9.0-android
```
Navigate to a saved recipe detail page and verify:
- Ingredients appear in a tonal card (`#14142E`) with circle indicators
- Tapping an ingredient toggles strikethrough + fades it
- Method steps appear in a tonal card with numbered badges (1, 2, 3...)
- No regression on header, image, metadata chips, description, or nutrition sections
