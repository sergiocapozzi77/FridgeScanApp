# Grouped Recipe Instructions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Preserve `HowToSection` grouping from JSON-LD recipes through the full import-to-display pipeline, rendering section headers in recipe visualizers.

**Architecture:** New `InstructionSection` model (`Name?` + `Steps`) replaces `List<string>` in all data models. `JsonLdRecipeExtractor` detects `HowToSection` objects instead of dumping raw JSON. Other extractors wrap flat steps into a single unnamed section. View models flatten sections into a `DisplaySteps` list with `MethodStep.IsSectionHeader`. XAML DataTemplates toggle between section header labels and step rows via `IsVisible` + `InvertedBoolConverter`.

**Tech Stack:** .NET MAUI, Newtonsoft.Json (extraction), System.Text.Json (Appwrite persistence), CommunityToolkit.Maui.InvertedBoolConverter

---

### Task 1: Create InstructionSection model

**Files:**
- Create: `FridgeScan/Models/InstructionSection.cs`

- [ ] **Step 1: Create the model**

```csharp
using System.Text.Json.Serialization;

namespace FridgeScan.Models;

public class InstructionSection
{
    public string? Name { get; set; }
    public List<string> Steps { get; set; } = new();
}
```

- [ ] **Step 2: Commit**

```sh
git add FridgeScan/Models/InstructionSection.cs
git commit -m "feat: add InstructionSection model for grouped recipe instructions"
```

---

### Task 2: Add IsSectionHeader to MethodStep model

**Files:**
- Modify: `FridgeScan/Models/MethodStep.cs`

- [ ] **Step 1: Add `IsSectionHeader` and `IsStep` properties**

```csharp
namespace FridgeScan.Models;

public class MethodStep
{
    public int Number { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? StepDuration { get; set; }
    public bool HasDuration => !string.IsNullOrEmpty(StepDuration);
    public bool IsSectionHeader { get; set; }
    public bool IsStep => !IsSectionHeader;
}
```

- [ ] **Step 2: Commit**

```sh
git add FridgeScan/Models/MethodStep.cs
git commit -m "feat: add IsSectionHeader and IsStep to MethodStep model"
```

---

### Task 3: Update RecipeExtractionResult MethodSteps type

**Files:**
- Modify: `FridgeScan/Services/RecipeImport/RecipeExtractionResult.cs`

- [ ] **Step 1: Change `MethodSteps` type**

Change line 18 from:
```csharp
public List<string>? MethodSteps { get; set; }
```
to:
```csharp
public List<InstructionSection>? MethodSteps { get; set; }
```

- [ ] **Step 2: Commit**

```sh
git add FridgeScan/Services/RecipeImport/RecipeExtractionResult.cs
git commit -m "refactor: change MethodSteps to List<InstructionSection> in RecipeExtractionResult"
```

---

### Task 4: Update SavedRecipe and RecipeSuggestion types

**Files:**
- Modify: `FridgeScan/Models/SavedRecipe.cs`
- Modify: `FridgeScan/Models/Recipe.cs`

- [ ] **Step 1: SavedRecipe.cs — line 20**

Change:
```csharp
public List<string> MethodSteps { get; set; } = new();
```
to:
```csharp
public List<InstructionSection> MethodSteps { get; set; } = new();
```

- [ ] **Step 2: Recipe.cs (RecipeSuggestion) — line 23**

Change:
```csharp
public List<string> MethodSteps { get; set; } = new();
```
to:
```csharp
public List<InstructionSection> MethodSteps { get; set; } = new();
```

- [ ] **Step 3: Commit**

```sh
git add FridgeScan/Models/SavedRecipe.cs FridgeScan/Models/Recipe.cs
git commit -m "refactor: change SavedRecipe/RecipeSuggestion MethodSteps to List<InstructionSection>"
```

---

### Task 5: Update JsonLdRecipeExtractor with HowToSection support

**Files:**
- Modify: `FridgeScan/Services/RecipeImport/JsonLdRecipeExtractor.cs`

- [ ] **Step 1: Replace inline extraction block and ExtractInstructionText method**

Replace lines 50-56 (the `if (schema["recipeInstructions"] is JArray instructions)` block and the `ExtractInstructionText` method at lines 170-179) with:

```csharp
if (schema["recipeInstructions"] is JArray instructions)
{
    result.MethodSteps = ExtractInstructions(instructions);
}
```

And replace the entire `ExtractInstructionText` method:

```csharp
private static List<InstructionSection>? ExtractInstructions(JArray instructions)
{
    var sections = new List<InstructionSection>();

    foreach (var item in instructions)
    {
        // HowToSection: has name + itemListElement array of HowToStep
        if (item is JObject obj && obj["itemListElement"] is JArray subSteps)
        {
            var section = new InstructionSection
            {
                Name = SanitizeText(SafeString(obj["name"])),
                Steps = subSteps
                    .Select(ExtractSingleStepText)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList()
            };
            if (section.Steps.Count > 0)
                sections.Add(section);
            continue;
        }

        // HowToStep with text, or plain string
        var stepText = ExtractSingleStepText(item);
        if (!string.IsNullOrWhiteSpace(stepText))
        {
            // Coalesce into the last unnamed section if one exists
            var last = sections.LastOrDefault();
            if (last != null && last.Name == null)
                last.Steps.Add(stepText);
            else
                sections.Add(new InstructionSection { Name = null, Steps = new List<string> { stepText } });
        }
    }

    return sections.Count > 0 ? sections : null;
}

private static string ExtractSingleStepText(JToken step)
{
    if (step is JObject obj && obj.TryGetValue("text", out var textToken))
        return SanitizeText(SafeString(textToken));

    if (step is JValue jv)
        return SanitizeText(jv.ToString());

    return SanitizeText(step?.ToString());
}
```

- [ ] **Step 2: Commit**

```sh
git add FridgeScan/Services/RecipeImport/JsonLdRecipeExtractor.cs
git commit -m "feat: handle HowToSection in JsonLdRecipeExtractor with grouped instructions"
```

---

### Task 6: Update remaining extractors to wrap flat steps

**Files:**
- Modify: `FridgeScan/Services/RecipeImport/MicrodataRecipeExtractor.cs`
- Modify: `FridgeScan/Services/RecipeImport/PostContentRecipeExtractor.cs`
- Modify: `FridgeScan/Services/RecipeImport/NextDataRecipeExtractor.cs`

- [ ] **Step 1: MicrodataRecipeExtractor — line 77**

Change:
```csharp
result.MethodSteps = steps;
```
to:
```csharp
result.MethodSteps = steps.Count > 0
    ? new List<InstructionSection> { new() { Steps = steps } }
    : null;
```

- [ ] **Step 2: PostContentRecipeExtractor — line 84**

Change:
```csharp
result.MethodSteps = steps;
```
to:
```csharp
result.MethodSteps = steps.Count > 0
    ? new List<InstructionSection> { new() { Steps = steps } }
    : null;
```

- [ ] **Step 3: NextDataRecipeExtractor — change ParseMethodSteps return type and body**

Change the return type of `ParseMethodSteps` (line 109) from `List<string>` to `List<InstructionSection>?` and wrap the result:

```csharp
private static List<InstructionSection>? ParseMethodSteps(JToken? method)
{
    var steps = new List<string>();
    if (method is not JArray sections) return null;

    foreach (var section in sections)
    {
        if (section["steps"] is not JArray stepItems) continue;
        foreach (var step in stepItems)
        {
            var text = SanitizeText(SafeString(step["description"]) ?? SafeString(step["text"]));
            if (!string.IsNullOrWhiteSpace(text))
                steps.Add(text);
        }
    }

    return steps.Count > 0
        ? new List<InstructionSection> { new() { Steps = steps } }
        : null;
}
```

- [ ] **Step 4: Commit**

```sh
git add FridgeScan/Services/RecipeImport/MicrodataRecipeExtractor.cs FridgeScan/Services/RecipeImport/PostContentRecipeExtractor.cs FridgeScan/Services/RecipeImport/NextDataRecipeExtractor.cs
git commit -m "refactor: wrap flat steps into unnamed InstructionSection in all extractors"
```

---

### Task 7: Update RecipeImportService

**Files:**
- Modify: `FridgeScan/Services/RecipeImport/RecipeImportService.cs`

- [ ] **Step 1: Update RecipeSuggestion construction for new type**

Line 59, change:
```csharp
MethodSteps = merged.MethodSteps ?? new List<string>(),
```
to:
```csharp
MethodSteps = merged.MethodSteps ?? new List<InstructionSection>(),
```

- [ ] **Step 2: Commit**

```sh
git add FridgeScan/Services/RecipeImport/RecipeImportService.cs
git commit -m "refactor: update RecipeImportService for List<InstructionSection>"
```

---

### Task 8: Update FavouriteService for backward-compatible deserialization

**Files:**
- Modify: `FridgeScan/Services/FavouriteService.cs`

- [ ] **Step 1: Add GetInstructionSections method**

Add these two methods after the existing `GetStringList` method (after line 214):

```csharp
private static List<InstructionSection> GetInstructionSections(AppwriteRow row, string key)
{
    if (row.Data.TryGetValue(key, out var el) && el.ValueKind == System.Text.Json.JsonValueKind.Array)
    {
        using var enumerator = el.EnumerateArray();

        if (!enumerator.MoveNext())
            return new List<InstructionSection>();

        var first = enumerator.Current;

        // Old format: array of plain strings → wrap in single unnamed section
        if (first.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            var steps = new List<string>();
            steps.Add(first.GetString() ?? string.Empty);
            while (enumerator.MoveNext())
                steps.Add(enumerator.Current.GetString() ?? string.Empty);
            return new List<InstructionSection> { new() { Steps = steps } };
        }

        // New format: array of section objects [{Name, Steps}, ...]
        if (first.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            var sections = new List<InstructionSection>();
            sections.Add(ReadSectionObject(first));
            while (enumerator.MoveNext())
                sections.Add(ReadSectionObject(enumerator.Current));
            return sections;
        }
    }
    return new List<InstructionSection>();
}

private static InstructionSection ReadSectionObject(System.Text.Json.JsonElement obj)
{
    var name = obj.TryGetProperty("Name", out var n) ? n.GetString() : null;
    var steps = new List<string>();
    if (obj.TryGetProperty("Steps", out var s) && s.ValueKind == System.Text.Json.JsonValueKind.Array)
    {
        foreach (var step in s.EnumerateArray())
        {
            var str = step.GetString();
            if (str != null) steps.Add(str);
        }
    }
    return new InstructionSection { Name = name, Steps = steps };
}
```

- [ ] **Step 2: Update MapToSavedRecipe to use the new method**

Line 190, change:
```csharp
MethodSteps = GetStringList(row, "methodSteps")
```
to:
```csharp
MethodSteps = GetInstructionSections(row, "methodSteps")
```

- [ ] **Step 3: Remove unused GetStringList method if it becomes dead code**

Check if `GetStringList` is still used anywhere. It's used for `cookbookIds` and `ingredients` (lines 188-189), so keep it.

- [ ] **Step 4: Commit**

```sh
git add FridgeScan/Services/FavouriteService.cs
git commit -m "feat: backward-compatible InstructionSection deserialization in FavouriteService"
```

---

### Task 9: Update SharedRecipeViewModel for sectioned display

**Files:**
- Modify: `FridgeScan/ViewModels/SharedRecipeViewModel.cs`

- [ ] **Step 1: Add DisplaySteps collection**

After line 29 (`private bool isSaving;`), add:
```csharp
public ObservableCollection<MethodStep> DisplaySteps { get; } = new();
```

- [ ] **Step 2: Add BuildDisplaySteps method**

```csharp
private void BuildDisplaySteps()
{
    DisplaySteps.Clear();
    if (ImportedRecipe?.MethodSteps == null) return;
    int stepNumber = 1;
    foreach (var section in ImportedRecipe.MethodSteps)
    {
        if (!string.IsNullOrWhiteSpace(section.Name))
            DisplaySteps.Add(new MethodStep { Text = section.Name, IsSectionHeader = true });
        foreach (var step in section.Steps)
        {
            DisplaySteps.Add(new MethodStep { Number = stepNumber++, Text = step });
        }
    }
}
```

- [ ] **Step 3: Call BuildDisplaySteps after import**

In `ImportRecipeAsync`, after line 118 (`HasRecipe = true;`), add:
```csharp
BuildDisplaySteps();
```

- [ ] **Step 4: Commit**

```sh
git add FridgeScan/ViewModels/SharedRecipeViewModel.cs
git commit -m "feat: add DisplaySteps with section headers to SharedRecipeViewModel"
```

---

### Task 10: Update SharedRecipePage XAML for grouped rendering

**Files:**
- Modify: `FridgeScan/Views/SharedRecipePage.xaml`

- [ ] **Step 1: Add toolkit namespace and converter resource**

Add `xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"` to the ContentPage attributes.

Add a page-level resource for the converter (after the opening `ContentPage` tag, before the Grid):

```xml
<ContentPage.Resources>
    <ResourceDictionary>
        <toolkit:InvertedBoolConverter x:Key="InvertBool" />
    </ResourceDictionary>
</ContentPage.Resources>
```

- [ ] **Step 2: Replace the Method Steps CollectionView**

Replace lines 68-78 (Label "Method" through the closing CollectionView tag) with:

```xml
<Label FontSize="16" FontAttributes="Bold" Text="Method" Margin="0,8,0,0" />
<CollectionView ItemsSource="{Binding DisplaySteps}" SelectionMode="None">
    <CollectionView.ItemTemplate>
        <DataTemplate x:DataType="models:MethodStep">
            <Grid>
                <!-- Section header -->
                <VerticalStackLayout IsVisible="{Binding IsSectionHeader}" Padding="4,8,4,4">
                    <Label Text="{Binding Text}" FontSize="15" FontAttributes="Bold"
                           TextColor="#8888AA" />
                </VerticalStackLayout>
                <!-- Step row (bullet) -->
                <Grid IsVisible="{Binding IsSectionHeader, Converter={StaticResource InvertBool}}"
                      ColumnDefinitions="Auto,*" Padding="4,4" ColumnSpacing="8">
                    <Label Grid.Column="0" FontSize="14" Text="&#x2022;"
                           VerticalOptions="Start" />
                    <Label Grid.Column="1" FontSize="14" Text="{Binding Text}"
                           LineBreakMode="WordWrap" />
                </Grid>
            </Grid>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

- [ ] **Step 3: Commit**

```sh
git add FridgeScan/Views/SharedRecipePage.xaml
git commit -m "feat: render grouped instruction sections with headers in SharedRecipePage"
```

---

### Task 11: Update SavedRecipeDetailViewModel for sectioned display

**Files:**
- Modify: `FridgeScan/ViewModels/SavedRecipeDetailViewModel.cs`

- [ ] **Step 1: Update BuildMethodSteps to emit section headers**

Replace the existing `BuildMethodSteps` method (lines 259-265):

```csharp
private void BuildMethodSteps()
{
    MethodSteps.Clear();
    if (Recipe?.MethodSteps == null) return;
    int stepNumber = 1;
    foreach (var section in Recipe.MethodSteps)
    {
        if (!string.IsNullOrWhiteSpace(section.Name))
            MethodSteps.Add(new MethodStep { Text = section.Name, IsSectionHeader = true });
        foreach (var step in section.Steps)
        {
            MethodSteps.Add(new MethodStep { Number = stepNumber++, Text = step });
        }
    }
}
```

- [ ] **Step 2: Commit**

```sh
git add FridgeScan/ViewModels/SavedRecipeDetailViewModel.cs
git commit -m "feat: emit section headers in SavedRecipeDetailViewModel.BuildMethodSteps"
```

---

### Task 12: Update SavedRecipeDetailPage XAML for section headers

**Files:**
- Modify: `FridgeScan/Views/SavedRecipeDetailPage.xaml`

- [ ] **Step 1: Add toolkit namespace and converter resource**

Add `xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"` to the ContentPage attributes.

Add page-level resources before the Grid:

```xml
<ContentPage.Resources>
    <ResourceDictionary>
        <toolkit:InvertedBoolConverter x:Key="InvertBool" />
    </ResourceDictionary>
</ContentPage.Resources>
```

- [ ] **Step 2: Update the Method CollectionView DataTemplate**

Replace the DataTemplate inside the Method steps CollectionView (lines 315-378) to toggle between section header and step card:

```xml
<CollectionView.ItemTemplate>
    <DataTemplate x:DataType="models:MethodStep">

        <!-- Section header -->
        <VerticalStackLayout IsVisible="{Binding IsSectionHeader}" Padding="0,8,0,4">
            <Label Text="{Binding Text}" FontSize="15" FontAttributes="Bold"
                   TextColor="#8888AA" />
        </VerticalStackLayout>

        <!-- Step card -->
        <Border
            Margin="0,0,0,10"
            Padding="16"
            BackgroundColor="#131629"
            StrokeThickness="0"
            StrokeShape="RoundRectangle 20"
            IsVisible="{Binding IsSectionHeader, Converter={StaticResource InvertBool}}">

            <Grid
                ColumnDefinitions="44,*,Auto"
                ColumnSpacing="14">

                <!-- Step number badge -->
                <Border
                    WidthRequest="40"
                    HeightRequest="40"
                    BackgroundColor="#2D2B6B"
                    StrokeThickness="0"
                    StrokeShape="RoundRectangle 10"
                    VerticalOptions="Start">
                    <Label
                        Text="{Binding Number}"
                        FontAttributes="Bold"
                        FontSize="16"
                        TextColor="White"
                        HorizontalOptions="Center"
                        VerticalOptions="Center" />
                </Border>

                <!-- Step text -->
                <Label
                    Grid.Column="1"
                    Text="{Binding Text}"
                    FontSize="15"
                    TextColor="#DDDDDD"
                    LineBreakMode="WordWrap"
                    VerticalOptions="Center" />

                <!-- Timer -->
                <VerticalStackLayout
                    Grid.Column="2"
                    Spacing="2"
                    VerticalOptions="Start"
                    IsVisible="{Binding HasDuration}">
                    <Label
                        FontFamily="Material"
                        Text="&#xe425;"
                        FontSize="16"
                        TextColor="#888888"
                        HorizontalOptions="Center" />
                    <Label
                        Text="{Binding StepDuration}"
                        FontSize="12"
                        TextColor="#888888"
                        HorizontalOptions="Center" />
                </VerticalStackLayout>

            </Grid>

        </Border>

    </DataTemplate>
</CollectionView.ItemTemplate>
```

**Crucial fix:** DataTemplate must have a *single root element*. Wrap both the section header and the step card in a Grid:

```xml
<CollectionView.ItemTemplate>
    <DataTemplate x:DataType="models:MethodStep">
        <Grid>
            <!-- Section header -->
            <VerticalStackLayout IsVisible="{Binding IsSectionHeader}" Padding="0,8,0,4">
                <Label Text="{Binding Text}" FontSize="15" FontAttributes="Bold"
                       TextColor="#8888AA" />
            </VerticalStackLayout>

            <!-- Step card -->
            <Border
                Margin="0,0,0,10"
                Padding="16"
                BackgroundColor="#131629"
                StrokeThickness="0"
                StrokeShape="RoundRectangle 20"
                IsVisible="{Binding IsSectionHeader, Converter={StaticResource InvertBool}}">
                <!-- ... existing step card content ... -->
            </Border>
        </Grid>
    </DataTemplate>
</CollectionView.ItemTemplate>
```

- [ ] **Step 3: Commit**

```sh
git add FridgeScan/Views/SavedRecipeDetailPage.xaml
git commit -m "feat: render grouped instruction section headers in SavedRecipeDetailPage"
```

---

### Task 13: Update RecipePreviewViewModel (if reachable)

**Files:**
- Modify: `FridgeScan/ViewModels/RecipePreviewViewModel.cs`

- [ ] **Step 1: Update ApplyQueryAttributes for new type**

Change line 50 from:
```csharp
MethodSteps = GetStringList(query, "MethodSteps")
```
to:
```csharp
MethodSteps = GetInstructionSections(query, "MethodSteps")
```

Update `GetStringList` to `GetInstructionSections` — or parse the data from query params differently since Shell navigation passes objects differently.

If `MethodSteps` was passed as `List<string>` via navigation params, and now we need `List<InstructionSection>`, the caller also needs updating. Since `RecipePreviewPage` may not be actively navigated to (no `GoToAsync` call found), skip this task for now. Flag as TODO: update when RecipePreviewPage navigation is wired up.

- [ ] **Step 2: Commit (or skip)**

```sh
git add FridgeScan/ViewModels/RecipePreviewViewModel.cs
git commit -m "fix: update RecipePreviewViewModel for List<InstructionSection> MethodSteps"
```

---

### Task 14: Verify build

- [ ] **Step 1: Build the project**

```sh
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android 2>&1 | tail -30
```

Expected: Build succeeds with no errors. Warnings for pre-existing issues (nullable annotations, culture) are acceptable.

- [ ] **Step 2: If build fails, fix compilation errors**

Common issues:
- Missing `using FridgeScan.Models;` in files that reference `InstructionSection`
- `GetStringList` needs to remain for other fields (ingredients, cookbookIds)
- DataTemplate wrapping in XAML needs single root element

- [ ] **Step 3: Commit any build fixes**

```sh
git add -A
git commit -m "fix: resolve build errors after InstructionSection migration"
```
