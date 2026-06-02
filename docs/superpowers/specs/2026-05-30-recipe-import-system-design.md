# Recipe Import System Design

## Overview

Replace the monolithic `JsonLdParser` with a well-architected, extensible recipe import system. Multiple specialized extractors each handle one data format; an orchestrator runs them all and merges results. A separate ingredient parser converts raw ingredient strings into structured quantities/units/names.

## New files

All in `FridgeScan/Services/RecipeImport/`:

| File | Role |
|---|---|
| `IRecipeExtractor.cs` | Interface for recipe data extractors |
| `RecipeExtractor.cs` | Abstract base with shared utilities (sanitize, ISO 8601, imperial→metric, fractions) |
| `RecipeExtractionResult.cs` | Data model — all fields nullable, each extractor fills what it finds |
| `IRecipeImageExtractor.cs` | Interface for step image extraction |
| `RecipeImage.cs` | Image reference (Ref + Url) |
| `RecipeImageExtractor.cs` | Scans HTML for recipe step images |
| `IRecipeIngredientParser.cs` | Interface for structured ingredient parsing |
| `ParsedIngredient.cs` | Quantity (float?), Unit, Name, Notes |
| `RecipeIngredientParser.cs` | Converts raw ingredient strings into structured data |
| `JsonLdRecipeExtractor.cs` | Parses `<script type="application/ld+json">` with `@type: Recipe` |
| `MicrodataRecipeExtractor.cs` | Parses `[itemtype*="Recipe"]` with `itemprop` attributes |
| `NextDataRecipeExtractor.cs` | Parses `<script id="__NEXT_DATA__">` (Next.js SSR data) |
| `PostContentRecipeExtractor.cs` | Parses `<script id="__POST_CONTENT__">` (GoodFood-specific) |
| `RecipeImportService.cs` | Orchestrator — fetch HTML, run all extractors, merge results, parse ingredients, extract images |

## Interfaces

### IRecipeExtractor

```
int Priority { get; }
Task<RecipeExtractionResult> ExtractAsync(string html, Uri baseUrl);
```

Priority determines merge precedence: higher values win when multiple extractors provide the same field.

### IRecipeImageExtractor

```
Task<List<RecipeImage>> ExtractImagesAsync(string html, Uri baseUrl);
```

### IRecipeIngredientParser

```
List<ParsedIngredient> Parse(List<string> rawIngredients);
```

## Models

### RecipeExtractionResult

All fields nullable. Each extractor fills only what it finds.

| Field | Type |
|---|---|
| Success | `bool` |
| Name | `string?` |
| Description | `string?` |
| ImageUrl | `string?` |
| Author | `string?` |
| PrepTime | `string?` |
| CookTime | `string?` |
| TotalTime | `string?` |
| Servings | `string?` |
| Difficulty | `string?` |
| RatingValue | `float?` |
| RatingCount | `int?` |
| Ingredients | `List<string>?` |
| MethodSteps | `List<string>?` |
| Nutritions | `List<string>?` |
| IsPremium | `bool` |
| ContentType | `string?` |
| RecipeSource | `string?` |

### ParsedIngredient

| Field | Type | Example |
|---|---|---|
| Quantity | `float?` | 1.5 |
| Unit | `string?` | "cups" |
| Name | `string` | "flour" |
| Notes | `string?` | "sifted" |

### RecipeImage

| Field | Type |
|---|---|
| Ref | `string` |
| Url | `string` |

## Extractors

### JsonLdRecipeExtractor (Priority: 100)

Parses `<script type="application/ld+json">`, finds objects with `@type: Recipe` (or `@graph` containing one, or `mainEntity`). Extracts all schema.org Recipe fields.

### NextDataRecipeExtractor (Priority: 80)

Parses `<script id="__NEXT_DATA__">`. Navigates `props.pageProps.schema` for recipe data, falls back to `props.pageProps.title`, `props.pageProps.servings`, etc.

### PostContentRecipeExtractor (Priority: 70)

Parses `<script id="__POST_CONTENT__">`. Extracts `title`, `ingredients[]`, `method[]`, `skillLevel`.

### MicrodataRecipeExtractor (Priority: 60)

Parses HTML elements with `[itemtype*="Recipe"]`. Extracts `itemprop="name"`, `itemprop="recipeIngredient"`, `itemprop="recipeInstructions"`, and meta tags for prepTime/cookTime.

## RecipeImportService (Orchestrator)

```
ImportFromUrlAsync(string url) → RecipeSuggestion:
  1. FetchHtmlAsync(url)
  2. Run all 4 IRecipeExtractors in parallel (Task.WhenAll)
  3. Merge results by priority — first non-null/non-empty wins
  4. If merged has raw ingredients, run RecipeIngredientParser
  5. Run RecipeImageExtractor
  6. Return populated RecipeSuggestion
```

## Ingredient Parsing

`RecipeIngredientParser.Parse(List<string>)` walks each raw string through these steps:

1. Normalize unicode/HTML fractions: `½` → 0.5, `&frac14;` → 0.25
2. Try to match a leading quantity pattern: `250`, `1.5`, `1 ½`, `¼`
3. Detect and split affixed unit: `250g` → qty=250, unit="g"
4. Match known unit after quantity: g, kg, ml, l, oz, lb, cups, tbsp, tsp, pcs, etc.
5. Remainder is the ingredient name
6. Handle "no quantity" cases: `"Salt to taste"` → qty=null, name="Salt", notes="to taste"

## Merge Strategy

For each field in `RecipeExtractionResult`, take the first non-null/non-empty value from extractors ordered by descending priority:

```
results.OrderByDescending(e => e.Priority)
  → For each field: if current value is null/empty, use next extractor's value
  → Final merged result
```

`Success` is true if at least one extractor found `ingredients` or `methodSteps`.

## Integration with existing code

- `SharedRecipeViewModel` calls `RecipeImportService.ImportFromUrlAsync(url)`
- Existing `JsonLdParser` is kept (still used by `RecipeGoodFoodService.GetFullRecipeDetailsAsync`)
- New code does NOT modify existing `IRecipeService` or `RecipeGoodFoodService`
- DI registrations in `MauiProgram.cs` for all new services

## What is NOT included

- AI-powered recipe structuring (phase grouping) — future iteration
- Appwrite caching/storage from the reference implementation — future iteration
- DeepSeek/LLM integration for import — future iteration
- Modifications to existing `RecipeSuggestion` model (only new `ParsedIngredient` added)
