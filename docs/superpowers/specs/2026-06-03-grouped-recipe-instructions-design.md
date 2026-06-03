# Grouped Recipe Instruction Sections

**Date:** 2026-06-03
**Status:** Draft

## Problem

When importing recipes from sites like recipetineats.com, the JSON-LD structured data uses `HowToSection` objects to group recipe instructions into named sections (e.g., "For the rice:", "For the chicken:"). The current import pipeline flattens all instructions into a `List<string>`, losing the section grouping entirely. For `HowToSection` objects, `ExtractInstructionText` falls through to `ToString()`, dumping raw JSON into the displayed step list.

## Solution Overview

Add an `InstructionSection` model to preserve section grouping through the full import-to-display pipeline. Each section has an optional name and a list of step texts. All three recipe visualizers render section headers appropriately.

## Data Model

### New: `InstructionSection`

```csharp
public class InstructionSection
{
    public string? Name { get; set; }  // Section header (e.g., "For the rice:")
    public List<string> Steps { get; set; } = new();
}
```

### Changes to existing types

| Type | Current | New |
|------|---------|-----|
| `RecipeExtractionResult.MethodSteps` | `List<string>?` | `List<InstructionSection>?` |
| `RecipeSuggestion.MethodSteps` | `List<string>` | `List<InstructionSection>` |
| `SavedRecipe.MethodSteps` | `List<string>` | `List<InstructionSection>` |

### Changes to `MethodStep`

```csharp
public class MethodStep
{
    public int Number { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? StepDuration { get; set; }
    public bool HasDuration => !string.IsNullOrEmpty(StepDuration);
    public bool IsSectionHeader { get; set; }  // NEW
}
```

## Extractor Changes

### `JsonLdRecipeExtractor`

- `ExtractInstructionText` → replaced with `ExtractInstructions(JArray instructions)`
- Detect each element's `@type` or shape:
  - `HowToSection` (has `name` + `itemListElement` array): create one `InstructionSection` with the name, extract each step's `text` from `itemListElement`
  - `HowToStep` or plain string: create a single unnamed `InstructionSection` containing all steps
  - JValue (string directly): create a single unnamed section

### Other extractors (`MicrodataRecipeExtractor`, `PostContentRecipeExtractor`, `NextDataRecipeExtractor`)

- Wrap their existing flat step lists into a single `InstructionSection` with `Name = null`
- Minimal change — just the return type wrapping

### `RecipeImportService.MergeResults`

- Takes first non-empty `MethodSteps` (already works, just the type changes)

## Visual Design

### `SavedRecipeDetailPage` (numbered card view)

```
Method
                  ─────────────────
── For the rice: ──│ Section header │
                  ─────────────────
[1] Wash rice well...
[2] Soak for 30 minutes, then drain
                  ─────────────────
── For the chicken: ──│ Section header │
                  ─────────────────
[3] Marinate chicken with yogurt and spices
[4] Cook on high heat until golden
```

- Steps numbered **globally** across all sections (1, 2, 3, 4 — never resets)
- Section headers rendered as Labels in the single CollectionView
- Implementation: `MethodStep.IsSectionHeader` flag — DataTemplate toggles between "section header label" and "step card border" via `IsVisible`
- **No-section fallback**: When there's a single unnamed section, no section header is shown — looks identical to current behavior

### `RecipePreviewPage` (save-to-cookbook preview)

```
Method
  For the rice:
    Wash rice well...
    Soak for 30 min...

  For the chicken:
    Marinate chicken...
    Cook chicken...
```

- Section name: bold, indented
- Steps: indented further, no bullets
- No-section fallback: renders flat, same as today

### `SharedRecipePage` (share extension bullet list)

```
Method
  For the rice:
    • Wash rice well...
    • Soak for 30 min...

  For the chicken:
    • Marinate chicken...
    • Cook chicken...
```

- Section name: bold
- Steps: bulleted, indented
- No-section fallback: renders flat bullets, same as today

### Visual style tokens (section headers)

| Property | Value |
|----------|-------|
| Font size | 15sp |
| Font weight | Bold |
| Color | `#8888AA` |
| Margin top | 12dp |
| Margin bottom | 4dp |

## Persistence

`FavouriteService` serializes/deserializes `SavedRecipe.MethodSteps` as `List<InstructionSection>`. Appwrite stores it as a JSON array of section objects:

```json
[
  { "Name": "For the rice:", "Steps": ["Wash well...", "Soak 30 min..."] },
  { "Name": null, "Steps": ["Marinate...", "Cook..."] }
]
```

Backward compatibility: recipes stored before this change have `methodSteps` as a JSON array of strings. On read, if the first element is a string (not an object), the deserialization wraps them into a single unnamed section.

## Files touched

1. `Models/InstructionSection.cs` — NEW
2. `Models/MethodStep.cs` — add `IsSectionHeader`
3. `Models/SavedRecipe.cs` — change `MethodSteps` type
4. `Models/Recipe.cs` — change `RecipeSuggestion.MethodSteps` type
5. `Services/RecipeImport/RecipeExtractionResult.cs` — change `MethodSteps` type
6. `Services/RecipeImport/RecipeImportService.cs` — type change
7. `Services/RecipeImport/JsonLdRecipeExtractor.cs` — `ExtractInstructions()` handles `HowToSection`
8. `Services/RecipeImport/MicrodataRecipeExtractor.cs` — wrap steps in one section
9. `Services/RecipeImport/PostContentRecipeExtractor.cs` — wrap steps in one section
10. `Services/RecipeImport/NextDataRecipeExtractor.cs` — wrap steps in one section
11. `Services/FavouriteService.cs` — fix deserialization for backward compat
12. `ViewModels/SavedRecipeDetailViewModel.cs` — `BuildMethodSteps()` emits section headers
13. `Views/SavedRecipeDetailPage.xaml` — render section headers
14. `Views/RecipePreviewPage.xaml` — render sections
15. `Views/SharedRecipePage.xaml` — render sections

## Scope boundaries

- **Out of scope**: Section grouping for AI-generated recipes (RecipeAiService). AI-generated steps are always flat — no section data to lose.
- **Out of scope**: Ingredient section grouping. Many recipe sites also group ingredients by section (e.g., "For the marinade:", "For the sauce:"). Only method instructions are handled here.
- **Out of scope**: `RecipeService` implementations (goodfood, giallozafferano) — they use their own `IRecipeService` interface, not the import pipeline.
