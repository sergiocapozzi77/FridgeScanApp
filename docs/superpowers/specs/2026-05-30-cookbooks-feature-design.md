# Cookbooks Feature Design

## Summary

Add a new **Cookbooks** tab to the app where users can browse, create, and manage cookbooks. Each cookbook contains saved recipes stored in Appwrite. Users can import recipes from external sources and save them into cookbooks.

## Appwrite Tables

### `cookbooks` collection
| Column | Type | Notes |
|--------|------|-------|
| `$id` | string | Auto-generated |
| `name` | varchar(255) | Required |
| `$createdAt` | datetime | Auto |
| `$updatedAt` | datetime | Auto |

### `recipes` collection
| Column | Type | Notes |
|--------|------|-------|
| `$id` | string | Auto-generated |
| `url` | text | Source URL, nullable |
| `name` | text | Recipe name, nullable |
| `cookbookIds[]` | varchar(50) | Array of cookbook IDs this recipe belongs to |
| `imageUrl` | text | Thumbnail image, nullable |
| `description` | text | Nullable |
| `difficulty` | varchar(20) | Nullable |
| `totalTime` | varchar(20) | Nullable |
| `recipeSource` | varchar(255) | e.g. "giallozafferano", "goodfood", nullable |
| `ingredients[]` | text | Array of ingredient strings |
| `methodSteps[]` | text | Array of step strings |
| `imageUrlBig` | text | Full-size image, nullable |

## Models

### `Cookbook`
```csharp
public class Cookbook
{
    public string RowId { get; set; }
    public string Name { get; set; }
    public int RecipeCount { get; set; } // computed client-side
}
```

### `SavedRecipe`
```csharp
public class SavedRecipe
{
    public string RowId { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageUrlBig { get; set; }
    public string? Description { get; set; }
    public string? Difficulty { get; set; }
    public string? TotalTime { get; set; }
    public string? RecipeSource { get; set; }
    public List<string> CookbookIds { get; set; } = new();
    public List<string> Ingredients { get; set; } = new();
    public List<string> MethodSteps { get; set; } = new();
}
```

## Services

### `CookbookService`

Follows the same Appwrite HTTP pattern as `ProductService` and `ActivityService` (direct REST calls with API key auth, `X-Appwrite-Project` and `X-Appwrite-Key` headers, paginated `limit`/`offset` JSON queries).

**Endpoints** (Appwrite Tables API: `{Endpoint}/v1/databases/{DatabaseId}/collections/{CollectionId}/documents`):

| Method | Purpose |
|--------|---------|
| `GetCookbooksAsync()` | List all cookbooks |
| `GetRecipesByCookbookIdAsync(string cookbookId)` | Query recipes where `cookbookIds` array contains the given ID |
| `GetAllSavedRecipesAsync()` | All recipes (for browsing unfiltered) |
| `GetRecipeByIdAsync(string recipeId)` | Single recipe by document ID |
| `CreateCookbookAsync(string name)` | POST new cookbook document |
| `DeleteCookbookAsync(string rowId)` | DELETE cookbook |
| `RenameCookbookAsync(string rowId, string name)` | PATCH name field |
| `SaveRecipeAsync(SavedRecipe recipe)` | POST new recipe document |
| `UpdateRecipeCookbooksAsync(string recipeId, List<string> cookbookIds)` | PATCH the `cookbookIds` array on a recipe |
| `DeleteRecipeAsync(string rowId)` | DELETE recipe from the recipes collection entirely |

### Integration with existing `RecipeImportService`

`SharedRecipeViewModel` currently imports a recipe via `RecipeImportService.ImportAsync(url)` and shows the result. After successful import, instead of the current "Close" flow, the app navigates to `RecipePreviewPage` passing the imported `RecipeSuggestion` data as query parameters. On save, `RecipePreviewViewModel` calls `CookbookService.SaveRecipeAsync()` and `UpdateRecipeCookbooksAsync()`.

## ViewModels

### `CookbookViewModel`
- **Extends**: `BaseViewModel`
- **DI**: `CookbookService cookbookService`
- **Properties**: `ObservableCollection<Cookbook> Cookbooks`, `bool IsLoading`
- **Commands**: `LoadCookbooksCommand`, `CreateCookbookCommand`, `RenameCookbookCommand(Cookbook)`, `DeleteCookbookCommand(Cookbook)`, `OpenCookbookCommand(Cookbook)`
- **OnAppearing**: Calls `LoadCookbooksAsync()` — fetches all cookbooks, then queries recipe counts for each

### `CookbookDetailViewModel`
- **Extends**: `BaseViewModel`, implements `IQueryAttributable`
- **DI**: `CookbookService cookbookService`
- **Properties**: `Cookbook? Cookbook`, `ObservableCollection<SavedRecipe> Recipes`, `bool IsLoading`
- **Commands**: `OpenRecipeCommand(SavedRecipe)`, `RenameCookbookCommand`, `DeleteCookbookCommand`, `EditRecipeCommand(SavedRecipe)` — shows action sheet with "Remove from cookbook" / "Move to another cookbook"
- **Query params**: receives `CookbookId` and `CookbookName`

### `RecipePreviewViewModel`
- **Extends**: `BaseViewModel`, implements `IQueryAttributable`
- **DI**: `CookbookService cookbookService`
- **Properties**: `SavedRecipe? Recipe`, `ObservableCollection<Cookbook> AllCookbooks`, `ObservableCollection<Cookbook> SelectedCookbooks`, `bool IsLoading`, `bool IsSaving`
- **Commands**: `SaveCommand`, `CreateAndAddCookbookCommand`, `ToggleCookbookSelectionCommand(Cookbook)`, `CloseCommand`
- **Query params**: receives serialized recipe data from the import flow (name, url, imageUrl, imageUrlBig, description, difficulty, totalTime, recipeSource, ingredients list, methodSteps list)
- **Save logic**: Calls `SaveRecipeAsync` with the selected cookbook IDs, then navigates back to the Cookbooks tab

### `SavedRecipeDetailViewModel`
- **Extends**: `BaseViewModel`, implements `IQueryAttributable`
- **DI**: `CookbookService cookbookService`
- **Properties**: `SavedRecipe? Recipe`, `bool IsLoading`
- **Commands**: `RemoveFromCookbookCommand`, `AddToAnotherCookbookCommand`
- **Query params**: receives `RecipeId`
- Loads full recipe details via `CookbookService.GetRecipeByIdAsync()`

## Pages

### `CookbookPage` — Tab content

- 2-column grid of cookbook cards using `CollectionView` with `ItemsLayout="VerticalGrid, 2"`
- Each card is an `SfCardView` containing:
  - **Mosaic area** (fixed aspect ratio, stacked images):
    - 0 recipes: placeholder icon with "No recipes yet"
    - 1 recipe: single full image
    - 2 recipes: two side-by-side images
    - 3 recipes: one large left (50% width), two stacked right
    - 4+ recipes: 2×2 grid (shows first 4)
  - **Cookbook name** (bold, max 2 lines)
  - **Recipe count** ("8 recipes")
- FAB-style circular "+" button for creating a new cookbook (prompts with text input dialog)
- Long-press gesture on card triggers context menu for rename/delete
- Pull-to-refresh using `SfPullToRefresh`

### `CookbookDetailPage` — Recipes inside a cookbook

- Header area with cookbook name, recipe count, and action buttons (Rename, Delete)
- Standard Shell back navigation
- Recipe list using `CollectionView` with horizontal card layout:
  - Image on left (match existing card pattern)
  - Text area: recipe name (bold), recipe source (e.g. "GialloZafferano"), time + difficulty
  - Menu button (⋯) on the right — tapping shows action sheet:
    - "Remove from cookbook"
    - "Move to another cookbook" (opens cookbook picker dialog)
- Tapping a recipe card navigates to `SavedRecipeDetailPage`
- Empty state: centered message when no recipes in cookbook

### `RecipePreviewPage` — Import review & save

- Standard Shell back/close navigation
- ScrollView containing:
  - Recipe image (large, aspect-fill) — falls back to placeholder if null
  - Recipe name (large, bold)
  - Source, time, difficulty metadata row
  - Description paragraph
  - **Ingredients** section header + vertical list inside a rounded card:
    - Each row: quantity+unit (aligned right, muted color) | ingredient name
    - Separators between rows
  - **Method** section header + numbered steps
- Sticky bottom panel ("Save to Cookbook"):
  - Multi-select cookbook chips — tap to toggle, selected chip is highlighted
  - "+ New Cookbook" chip with dashed style — tapping prompts for name and creates it on the spot, auto-selects it
  - "Save Recipe" button — disabled when no cookbook selected; calls save, then navigates to Cookbooks tab

### `SavedRecipeDetailPage` — View saved recipe details

- Content layout matches `RecipePreviewPage` (image, name, metadata, ingredients list, method steps)
- Read-only — no save panel
- Header menu button for "Remove from cookbook" / "Add to cookbook" actions
- Standard Shell back navigation

## Navigation

### AppShell changes

Add a 5th `<ShellContent>` to the `<TabBar>`:
```xml
<ShellContent Title="Cookbooks" Icon="{OnPlatform Default='icon_cookbook.png'}"
              ContentTemplate="{DataTemplate views:CookbookPage}" />
```

New route registrations in `AppShell.xaml.cs`:
```csharp
Routing.RegisterRoute(nameof(CookbookDetailPage), typeof(CookbookDetailPage));
Routing.RegisterRoute(nameof(RecipePreviewPage), typeof(RecipePreviewPage));
Routing.RegisterRoute(nameof(SavedRecipeDetailPage), typeof(SavedRecipeDetailPage));
```

### Flow diagram

```
CookbookPage (tab)
  └─ tap card ─→ CookbookDetailPage
       ├─ tap recipe ─→ SavedRecipeDetailPage
       └─ ⋯ menu ─→ Remove / Move to cookbook

SharedRecipePage (existing import modal)
  └─ import success ─→ RecipePreviewPage
       └─ choose cookbooks + Save ─→ CookbookPage (tab)
```

### Modification to existing `SharedRecipePage` flow

The `CloseCommand` in `SharedRecipeViewModel` currently just closes the modal. After import success, instead of only offering "Close", the page will navigate to `RecipePreviewPage` with the imported recipe data serialized as query parameters, then close itself.

## DI Registration

```csharp
// Service
builder.Services.AddSingleton<CookbookService>();

// ViewModels
builder.Services.AddSingleton<CookbookViewModel>();
builder.Services.AddSingleton<CookbookDetailViewModel>();
builder.Services.AddSingleton<RecipePreviewViewModel>();
builder.Services.AddSingleton<SavedRecipeDetailViewModel>();

// Pages
builder.Services.AddTransient<CookbookPage>();
builder.Services.AddTransient<CookbookDetailPage>();
builder.Services.AddTransient<RecipePreviewPage>();
builder.Services.AddTransient<SavedRecipeDetailPage>();
```

## Edge Cases

| Scenario | Handling |
|----------|----------|
| Cookbook with 0 recipes | Mosaic shows placeholder icon, "0 recipes" label |
| Cookbook with >4 recipes | Mosaic shows only the first 4 recipe images |
| Recipe image URL is null/missing | Gray placeholder with food icon in mosaic and detail views |
| Network error loading cookbooks | Show error state with retry button (match existing pattern) |
| Save recipe with no cookbook selected | "Save Recipe" button is disabled until at least one cookbook is selected |
| Recipe already in the selected cookbook | Server-side idempotent; client skips if `CookbookIds` already contains target |
| Delete a cookbook with recipes | Confirmation dialog: "Recipes will be kept but unlinked from this cookbook" |
| Delete the last remaining recipe | Recipe document deleted from Appwrite, cookbook shows 0 recipes |
| Creating a cookbook with empty/whitespace name | Client-side validation before POST, don't show empty names |
| Imported recipe has no image | Show placeholder in preview, still saveable |
| Rapid double-tap on "Save Recipe" | Disable button on first tap (set `IsSaving = true`), show spinner on button |
| Back navigation during save | Do not allow back navigation while `IsSaving` is true |

## Mosaic Rendering (Custom Control)

A `CookbookMosaic` `ContentView` with a `BindableProperty` for `IList<string> ImageUrls`:

```csharp
int imageCount = Math.Min(urls?.Count ?? 0, 4);

switch (imageCount)
{
    case 0: return EmptyPlaceholder;      // icon + "No recipes yet"
    case 1: return SingleImage(urls[0]);  // full area image
    case 2: return SideBySide(urls[0], urls[1]); // two 50% columns
    case 3: return OneLargeTwoStacked(urls[0], urls[1], urls[2]); // 50% left, 50% right split into two rows
    case 4: return TwoByTwoGrid(urls[0], urls[1], urls[2], urls[3]); // 2×2 grid
}
```

The control uses a `Grid` as its root and swaps child layouts based on `imageCount`. All images use `Aspect="AspectFill"` and fall back to a placeholder when the URL fails to load.
