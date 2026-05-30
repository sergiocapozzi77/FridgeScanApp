# Cookbooks Feature Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Cookbooks tab with cookbook cards (image mosaics), cookbook detail (recipe list), recipe preview/save after import, and saved recipe detail view — all backed by Appwrite Tables API.

**Architecture:** New `CookbookService` follows the existing `ProductService` Appwrite HTTP pattern. Four new pages with ViewModels using the same `BaseViewModel` + `[ObservableProperty]` + `[RelayCommand]` pattern. A custom `CookbookMosaic` ContentView handles the 0/1/2/3/4 image mosaic layouts. The existing `SharedRecipePage` flow is extended to navigate to `RecipePreviewPage` after import.

**Tech Stack:** .NET 9 MAUI, CommunityToolkit.Mvvm, Syncfusion MAUI (Cards, PullToRefresh, Buttons), Appwrite Tables API

---

## File Structure

| Action | File | Purpose |
|--------|------|---------|
| Create | `Models/Cookbook.cs` | Cookbook model |
| Create | `Models/SavedRecipe.cs` | Saved recipe model |
| Create | `Services/CookbookService.cs` | Appwrite CRUD for both tables |
| Create | `Views/CookbookMosaic.cs` | Custom control for mosaic images |
| Create | `ViewModels/CookbookViewModel.cs` | Cookbooks tab logic |
| Create | `Views/CookbookPage.xaml` + `.xaml.cs` | Cookbooks tab UI |
| Create | `ViewModels/CookbookDetailViewModel.cs` | Cookbook detail logic |
| Create | `Views/CookbookDetailPage.xaml` + `.xaml.cs` | Recipes in cookbook UI |
| Create | `ViewModels/RecipePreviewViewModel.cs` | Import preview + save logic |
| Create | `Views/RecipePreviewPage.xaml` + `.xaml.cs` | Import preview + save UI |
| Create | `ViewModels/SavedRecipeDetailViewModel.cs` | View saved recipe logic |
| Create | `Views/SavedRecipeDetailPage.xaml` + `.xaml.cs` | View saved recipe UI |
| Modify | `AppShell.xaml` | Add 5th tab |
| Modify | `AppShell.xaml.cs` | Register new routes |
| Modify | `MauiProgram.cs` | DI registrations |
| Modify | `FridgeScan.csproj` | Add MauiXaml entries |
| Modify | `Views/SharedRecipePage.xaml` | Add "Save to Cookbook" button |
| Modify | `ViewModels/SharedRecipeViewModel.cs` | Add navigation to preview |

---

### Task 1: Create Models

**Files:**
- Create: `FridgeScan/Models/Cookbook.cs`
- Create: `FridgeScan/Models/SavedRecipe.cs`

- [ ] **Step 1: Create Cookbook model**

```csharp
namespace FridgeScan.Models;

public class Cookbook
{
    public string RowId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RecipeCount { get; set; }
    public List<string> PreviewImageUrls { get; set; } = new();
}
```

- [ ] **Step 2: Create SavedRecipe model**

```csharp
using Newtonsoft.Json;

namespace FridgeScan.Models;

public class SavedRecipe
{
    public string RowId { get; set; } = string.Empty;
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

- [ ] **Step 3: Commit**

```bash
git add FridgeScan/Models/Cookbook.cs FridgeScan/Models/SavedRecipe.cs
git commit -m "feat: add Cookbook and SavedRecipe models"
```

---

### Task 2: Create CookbookService

**Files:**
- Create: `FridgeScan/Services/CookbookService.cs`

- [ ] **Step 1: Create CookbookService**

```csharp
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FridgeScan.Models;

namespace FridgeScan.Services;

public class CookbookService
{
    private readonly HttpClient _http;
    private const string Endpoint = "https://fra.cloud.appwrite.io/v1";
    private const string ProjectId = "6954045e003c75c1c3bf";
    private const string DatabaseId = "695404ac0021bf7d9707";
    private const string CookbooksCollectionId = "cookbooks";
    private const string RecipesCollectionId = "recipes";

    public CookbookService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("X-Appwrite-Project", ProjectId);
        _http.DefaultRequestHeaders.Add("X-Appwrite-Key", Secrets.AppWriteApiKey);
    }

    // --- Cookbooks ---

    public async Task<List<Cookbook>> GetCookbooksAsync()
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{CookbooksCollectionId}/rows";
            var allRows = await FetchAllRowsAsync(url);
            return allRows.Select(r => new Cookbook
            {
                RowId = r.Id,
                Name = r.GetProperty("name").GetString() ?? string.Empty
            }).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching cookbooks: {ex.Message}");
            return new List<Cookbook>();
        }
    }

    public async Task<Cookbook?> CreateCookbookAsync(string name)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{CookbooksCollectionId}/rows";
            var body = new
            {
                rowId = GenerateId(),
                data = new { name }
            };
            var response = await _http.PostAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();
            var row = await response.Content.ReadFromJsonAsync<AppwriteRow>();
            return row == null ? null : new Cookbook { RowId = row.Id, Name = name };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating cookbook: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteCookbookAsync(string rowId)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{CookbooksCollectionId}/rows/{rowId}";
            var response = await _http.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting cookbook: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RenameCookbookAsync(string rowId, string name)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{CookbooksCollectionId}/rows/{rowId}";
            var body = new { data = new { name } };
            var response = await _http.PatchAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error renaming cookbook: {ex.Message}");
            return false;
        }
    }

    // --- Recipes ---

    public async Task<List<SavedRecipe>> GetRecipesByCookbookIdAsync(string cookbookId)
    {
        try
        {
            var baseUrl = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{RecipesCollectionId}/rows";
            var query = $"{{\"method\":\"contains\",\"attribute\":\"cookbookIds\",\"values\":[\"{cookbookId}\"]}}";
            var encoded = new List<string> { $"queries[0]={Uri.EscapeDataString(query)}" };
            var allRows = await FetchAllRowsAsync(baseUrl, encoded);
            return allRows.Select(MapToSavedRecipe).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching recipes: {ex.Message}");
            return new List<SavedRecipe>();
        }
    }

    public async Task<List<SavedRecipe>> GetAllSavedRecipesAsync()
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{RecipesCollectionId}/rows";
            var allRows = await FetchAllRowsAsync(url);
            return allRows.Select(MapToSavedRecipe).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching all recipes: {ex.Message}");
            return new List<SavedRecipe>();
        }
    }

    public async Task<SavedRecipe?> GetRecipeByIdAsync(string recipeId)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{RecipesCollectionId}/rows/{recipeId}";
            var row = await _http.GetFromJsonAsync<AppwriteRow>(url);
            return row == null ? null : MapToSavedRecipe(row);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching recipe: {ex.Message}");
            return null;
        }
    }

    public async Task<SavedRecipe?> SaveRecipeAsync(SavedRecipe recipe)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{RecipesCollectionId}/rows";
            var body = new
            {
                rowId = GenerateId(),
                data = new
                {
                    url = recipe.Url ?? string.Empty,
                    name = recipe.Name ?? string.Empty,
                    cookbookIds = recipe.CookbookIds,
                    imageUrl = recipe.ImageUrl ?? string.Empty,
                    description = recipe.Description ?? string.Empty,
                    difficulty = recipe.Difficulty ?? string.Empty,
                    totalTime = recipe.TotalTime ?? string.Empty,
                    recipeSource = recipe.RecipeSource ?? string.Empty,
                    ingredients = recipe.Ingredients,
                    methodSteps = recipe.MethodSteps,
                    imageUrlBig = recipe.ImageUrlBig ?? string.Empty
                }
            };
            var response = await _http.PostAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();
            var row = await response.Content.ReadFromJsonAsync<AppwriteRow>();
            if (row != null) recipe.RowId = row.Id;
            return recipe;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving recipe: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateRecipeCookbooksAsync(string recipeId, List<string> cookbookIds)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{RecipesCollectionId}/rows/{recipeId}";
            var body = new { data = new { cookbookIds } };
            var response = await _http.PatchAsJsonAsync(url, body);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating recipe cookbooks: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteRecipeAsync(string rowId)
    {
        try
        {
            var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{RecipesCollectionId}/rows/{rowId}";
            var response = await _http.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting recipe: {ex.Message}");
            return false;
        }
    }

    // --- Helpers ---

    private async Task<List<AppwriteRow>> FetchAllRowsAsync(string baseUrl, List<string>? baseQueries = null)
    {
        baseQueries ??= new List<string>();
        var allRows = new List<AppwriteRow>();
        const int perPage = 100;
        int offset = 0;
        int total = int.MaxValue;

        while (allRows.Count < total)
        {
            var encoded = new List<string>(baseQueries);
            var idx = encoded.Count;
            encoded.Add($"queries[{idx}]={Uri.EscapeDataString($"{{\"method\":\"limit\",\"values\":[{perPage}]}}")}");
            encoded.Add($"queries[{idx + 1}]={Uri.EscapeDataString($"{{\"method\":\"offset\",\"values\":[{offset}]}}")}");

            var queryString = "?" + string.Join("&", encoded);
            var response = await _http.GetFromJsonAsync<AppwriteRowsResponse>(baseUrl + queryString);

            if (response?.Rows == null || response.Rows.Count == 0) break;
            if (total == int.MaxValue) total = response.Total;

            allRows.AddRange(response.Rows);
            offset = allRows.Count;
            if (allRows.Count >= total) break;
        }

        return allRows;
    }

    private static SavedRecipe MapToSavedRecipe(AppwriteRow row)
    {
        return new SavedRecipe
        {
            RowId = row.Id,
            Name = row.GetProperty("name").GetString(),
            Url = GetStringOrNull(row, "url"),
            ImageUrl = GetStringOrNull(row, "imageUrl"),
            ImageUrlBig = GetStringOrNull(row, "imageUrlBig"),
            Description = GetStringOrNull(row, "description"),
            Difficulty = GetStringOrNull(row, "difficulty"),
            TotalTime = GetStringOrNull(row, "totalTime"),
            RecipeSource = GetStringOrNull(row, "recipeSource"),
            CookbookIds = GetStringList(row, "cookbookIds"),
            Ingredients = GetStringList(row, "ingredients"),
            MethodSteps = GetStringList(row, "methodSteps")
        };
    }

    private static string? GetStringOrNull(AppwriteRow row, string key)
    {
        if (row.Data.TryGetProperty(key, out var el))
            return el.ValueKind == System.Text.Json.JsonValueKind.Null ? null : el.GetString();
        return null;
    }

    private static List<string> GetStringList(AppwriteRow row, string key)
    {
        if (row.Data.TryGetProperty(key, out var el) && el.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            var list = new List<string>();
            foreach (var item in el.EnumerateArray())
            {
                var s = item.GetString();
                if (s != null) list.Add(s);
            }
            return list;
        }
        return new List<string>();
    }

    private static string GenerateId(int length = 20)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var buffer = new char[length];
        buffer[0] = chars[random.Next(chars.Length)];
        for (int i = 1; i < length; i++)
            buffer[i] = chars[random.Next(chars.Length)];
        return new string(buffer);
    }

    public class AppwriteRowsResponse
    {
        public int Total { get; set; }
        public List<AppwriteRow> Rows { get; set; } = new();
    }

    public class AppwriteRow
    {
        [JsonPropertyName("$id")]
        public string Id { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonExtensionData]
        public Dictionary<string, System.Text.Json.JsonElement> Data { get; set; } = new();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add FridgeScan/Services/CookbookService.cs
git commit -m "feat: add CookbookService for Appwrite cookbooks and recipes tables"
```

---

### Task 3: Create CookbookMosaic Custom Control

**Files:**
- Create: `FridgeScan/Views/CookbookMosaic.cs`

- [ ] **Step 1: Create the custom control**

```csharp
namespace FridgeScan.Views;

public class CookbookMosaic : ContentView
{
    public static readonly BindableProperty ImageUrlsProperty =
        BindableProperty.Create(nameof(ImageUrls), typeof(IList<string>), typeof(CookbookMosaic),
            propertyChanged: OnImageUrlsChanged);

    public IList<string>? ImageUrls
    {
        get => (IList<string>?)GetValue(ImageUrlsProperty);
        set => SetValue(ImageUrlsProperty, value);
    }

    private readonly Grid _grid = new();

    public CookbookMosaic()
    {
        _grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        _grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        _grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        _grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        Content = _grid;
        RenderMosaic();
    }

    private static void OnImageUrlsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((CookbookMosaic)bindable).RenderMosaic();
    }

    private void RenderMosaic()
    {
        _grid.Children.Clear();
        var urls = ImageUrls;
        int count = urls?.Count ?? 0;

        switch (count)
        {
            case 0:
                _grid.Children.Add(new Label
                {
                    Text = "\U0001f372",
                    FontSize = 28,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Colors.Gray
                });
                Grid.SetRowSpan((View)_grid.Children[0], 2);
                Grid.SetColumnSpan((View)_grid.Children[0], 2);
                break;

            case 1:
                _grid.Children.Add(CreateImage(urls![0]));
                Grid.SetRowSpan((View)_grid.Children[0], 2);
                Grid.SetColumnSpan((View)_grid.Children[0], 2);
                break;

            case 2:
                _grid.Children.Add(CreateImage(urls![0]));
                Grid.SetRowSpan((View)_grid.Children[0], 2);
                _grid.Children.Add(CreateImage(urls![1]));
                Grid.SetColumn((View)_grid.Children[1], 1);
                Grid.SetRowSpan((View)_grid.Children[1], 2);
                break;

            case 3:
                _grid.Children.Add(CreateImage(urls![0]));
                Grid.SetRowSpan((View)_grid.Children[0], 2);
                _grid.Children.Add(CreateImage(urls![1]));
                Grid.SetColumn((View)_grid.Children[1], 1);
                _grid.Children.Add(CreateImage(urls![2]));
                Grid.SetColumn((View)_grid.Children[2], 1);
                Grid.SetRow((View)_grid.Children[2], 1);
                break;

            default: // 4+
                _grid.Children.Add(CreateImage(urls![0]));
                _grid.Children.Add(CreateImage(urls![1]));
                Grid.SetColumn((View)_grid.Children[1], 1);
                _grid.Children.Add(CreateImage(urls![2]));
                Grid.SetRow((View)_grid.Children[2], 1);
                _grid.Children.Add(CreateImage(urls![3]));
                Grid.SetRow((View)_grid.Children[3], 1);
                Grid.SetColumn((View)_grid.Children[3], 1);
                break;
        }
    }

    private static Image CreateImage(string url)
    {
        return new Image
        {
            Source = ImageSource.FromUri(new Uri(url)),
            Aspect = Aspect.AspectFill
        };
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add FridgeScan/Views/CookbookMosaic.cs
git commit -m "feat: add CookbookMosaic custom control for 0/1/2/3/4 image layouts"
```

---

### Task 4: Create CookbookViewModel

**Files:**
- Create: `FridgeScan/ViewModels/CookbookViewModel.cs`

- [ ] **Step 1: Create CookbookViewModel**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FridgeScan.Models;
using FridgeScan.Services;

namespace FridgeScan.ViewModels;

public partial class CookbookViewModel : BaseViewModel
{
    private readonly CookbookService _cookbookService;

    [ObservableProperty]
    private ObservableCollection<Cookbook> cookbooks = new();

    [ObservableProperty]
    private bool isLoading;

    public CookbookViewModel(CookbookService cookbookService)
    {
        _cookbookService = cookbookService;
    }

    [RelayCommand]
    private async Task LoadCookbooks()
    {
        if (IsLoading) return;
        try
        {
            IsLoading = true;
            var allCookbooks = await _cookbookService.GetCookbooksAsync();
            var allRecipes = await _cookbookService.GetAllSavedRecipesAsync();

            foreach (var cookbook in allCookbooks)
            {
                var cookbookRecipes = allRecipes
                    .Where(r => r.CookbookIds.Contains(cookbook.RowId))
                    .ToList();
                cookbook.RecipeCount = cookbookRecipes.Count;
                cookbook.PreviewImageUrls = cookbookRecipes
                    .Select(r => r.ImageUrl)
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Take(4)
                    .ToList()!;
            }

            Cookbooks = new ObservableCollection<Cookbook>(allCookbooks);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading cookbooks: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateCookbook()
    {
        var name = await Shell.Current.DisplayPromptAsync("New Cookbook", "Enter a name for the cookbook:", "Create", "Cancel");
        if (string.IsNullOrWhiteSpace(name)) return;

        var created = await _cookbookService.CreateCookbookAsync(name.Trim());
        if (created != null)
        {
            Cookbooks.Add(created);
        }
    }

    [RelayCommand]
    private async Task RenameCookbook(Cookbook cookbook)
    {
        var newName = await Shell.Current.DisplayPromptAsync("Rename", "Enter new name:", "Rename", "Cancel",
            initialValue: cookbook.Name);
        if (string.IsNullOrWhiteSpace(newName)) return;

        var success = await _cookbookService.RenameCookbookAsync(cookbook.RowId, newName.Trim());
        if (success)
        {
            cookbook.Name = newName.Trim();
            var index = Cookbooks.IndexOf(cookbook);
            if (index >= 0)
            {
                Cookbooks.RemoveAt(index);
                Cookbooks.Insert(index, cookbook);
            }
        }
    }

    [RelayCommand]
    private async Task DeleteCookbook(Cookbook cookbook)
    {
        var confirmed = await Shell.Current.DisplayAlert("Delete Cookbook",
            $"Delete \"{cookbook.Name}\"? Recipes will be kept but unlinked.", "Delete", "Cancel");
        if (!confirmed) return;

        var success = await _cookbookService.DeleteCookbookAsync(cookbook.RowId);
        if (success)
        {
            Cookbooks.Remove(cookbook);
        }
    }

    [RelayCommand]
    private async Task OpenCookbook(Cookbook cookbook)
    {
        var parameters = new Dictionary<string, object>
        {
            { "CookbookId", cookbook.RowId },
            { "CookbookName", cookbook.Name }
        };
        await Shell.Current.GoToAsync(nameof(Views.CookbookDetailPage), parameters);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add FridgeScan/ViewModels/CookbookViewModel.cs
git commit -m "feat: add CookbookViewModel with CRUD and navigation"
```

---

### Task 5: Create CookbookPage

**Files:**
- Create: `FridgeScan/Views/CookbookPage.xaml`
- Create: `FridgeScan/Views/CookbookPage.xaml.cs`

- [ ] **Step 1: Create CookbookPage.xaml**

```xml
<ContentPage
    x:Class="FridgeScan.Views.CookbookPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:sfCards="clr-namespace:Syncfusion.Maui.Cards;assembly=Syncfusion.Maui.Cards"
    xmlns:sfPull="clr-namespace:Syncfusion.Maui.PullToRefresh;assembly=Syncfusion.Maui.PullToRefresh"
    xmlns:sfBusy="clr-namespace:Syncfusion.Maui.Core;assembly=Syncfusion.Maui.Core"
    xmlns:views="clr-namespace:FridgeScan.Views"
    Title="Cookbooks">

    <Grid RowDefinitions="Auto,*">

        <!-- Header -->
        <Grid Grid.Row="0" Padding="16,12" ColumnDefinitions="*,Auto">
            <Label Text="Cookbooks" FontSize="24" FontAttributes="Bold" VerticalOptions="Center" />
            <Button Grid.Column="1"
                    Text="+"
                    FontSize="20"
                    FontAttributes="Bold"
                    BackgroundColor="#4a90d9"
                    TextColor="White"
                    CornerRadius="20"
                    WidthRequest="40"
                    HeightRequest="40"
                    Padding="0"
                    Command="{Binding CreateCookbookCommand}" />
        </Grid>

        <!-- Content -->
        <sfPull:SfPullToRefresh Grid.Row="1"
                                 IsRefreshing="{Binding IsLoading}"
                                 RefreshCommand="{Binding LoadCookbooksCommand}">
            <sfPull:SfPullToRefresh.PullableContent>
                <CollectionView ItemsSource="{Binding Cookbooks}"
                                ItemsLayout="VerticalGrid, 2"
                                SelectionMode="None">
                    <CollectionView.ItemTemplate>
                        <DataTemplate x:DataType="models:Cookbook"
                                      xmlns:models="clr-namespace:FridgeScan.Models">
                            <sfCards:SfCardView Margin="6" CornerRadius="10" Padding="0">
                                <sfCards:SfCardView.GestureRecognizers>
                                    <TapGestureRecognizer
                                        Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodel:CookbookViewModel}}, Path=OpenCookbookCommand}"
                                        CommandParameter="{Binding}" />
                                </sfCards:SfCardView.GestureRecognizers>
                                <VerticalStackLayout Padding="0" Spacing="0">
                                    <!-- Mosaic -->
                                    <views:CookbookMosaic
                                        ImageUrls="{Binding PreviewImageUrls}"
                                        HeightRequest="140" />
                                    <!-- Info -->
                                    <VerticalStackLayout Padding="10,8">
                                        <Label Text="{Binding Name}"
                                               FontSize="14"
                                               FontAttributes="Bold"
                                               LineBreakMode="TailTruncation"
                                               MaxLines="2" />
                                        <Label FontSize="12"
                                               TextColor="Gray">
                                            <Label.Text>
                                                <MultiBinding StringFormat="{}{0} recipes">
                                                    <Binding Path="RecipeCount" />
                                                </MultiBinding>
                                            </Label.Text>
                                        </Label>
                                    </VerticalStackLayout>
                                </VerticalStackLayout>
                            </sfCards:SfCardView>
                        </DataTemplate>
                    </CollectionView.ItemTemplate>
                </CollectionView>
            </sfPull:SfPullToRefresh.PullableContent>
        </sfPull:SfPullToRefresh>

        <!-- Loading overlay -->
        <sfBusy:SfBusyIndicator Grid.Row="1"
                                 AnimationType="CircularMaterial"
                                 IsRunning="{Binding IsLoading}"
                                 IsVisible="{Binding IsLoading}"
                                 HorizontalOptions="Center"
                                 VerticalOptions="Center"
                                 HeightRequest="60"
                                 WidthRequest="60" />
    </Grid>
</ContentPage>
```

- [ ] **Step 2: Create CookbookPage.xaml.cs**

```csharp
using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class CookbookPage : ContentPage
{
    private readonly CookbookViewModel _vm;

    public CookbookPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _vm = services.GetService<CookbookViewModel>()!;
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCookbooksCommand.Execute(null);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add FridgeScan/Views/CookbookPage.xaml FridgeScan/Views/CookbookPage.xaml.cs
git commit -m "feat: add CookbookPage with 2-column mosaic card grid"
```

---

### Task 6: Create CookbookDetailViewModel

**Files:**
- Create: `FridgeScan/ViewModels/CookbookDetailViewModel.cs`

- [ ] **Step 1: Create CookbookDetailViewModel**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FridgeScan.Models;
using FridgeScan.Services;

namespace FridgeScan.ViewModels;

public partial class CookbookDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly CookbookService _cookbookService;

    [ObservableProperty]
    private Cookbook? cookbook;

    [ObservableProperty]
    private ObservableCollection<SavedRecipe> recipes = new();

    [ObservableProperty]
    private bool isLoading;

    private string _cookbookId = string.Empty;

    public CookbookDetailViewModel(CookbookService cookbookService)
    {
        _cookbookService = cookbookService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("CookbookId", out var id))
        {
            _cookbookId = id?.ToString() ?? string.Empty;
        }
        if (query.TryGetValue("CookbookName", out var name))
        {
            Cookbook = new Cookbook
            {
                RowId = _cookbookId,
                Name = name?.ToString() ?? string.Empty
            };
        }
        _ = LoadRecipesAsync();
    }

    [RelayCommand]
    private async Task LoadRecipes()
    {
        if (IsLoading || string.IsNullOrEmpty(_cookbookId)) return;
        await LoadRecipesAsync();
    }

    private async Task LoadRecipesAsync()
    {
        try
        {
            IsLoading = true;
            var list = await _cookbookService.GetRecipesByCookbookIdAsync(_cookbookId);
            Recipes = new ObservableCollection<SavedRecipe>(list);
            if (Cookbook != null)
                Cookbook.RecipeCount = list.Count;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading recipes: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenRecipe(SavedRecipe recipe)
    {
        var parameters = new Dictionary<string, object>
        {
            { "RecipeId", recipe.RowId }
        };
        await Shell.Current.GoToAsync(nameof(Views.SavedRecipeDetailPage), parameters);
    }

    [RelayCommand]
    private async Task EditRecipe(SavedRecipe recipe)
    {
        var action = await Shell.Current.DisplayActionSheet(
            recipe.Name ?? "Recipe", "Cancel", null,
            "Remove from cookbook", "Move to another cookbook");

        switch (action)
        {
            case "Remove from cookbook":
                await RemoveRecipeFromCookbook(recipe);
                break;
            case "Move to another cookbook":
                await MoveRecipeToCookbook(recipe);
                break;
        }
    }

    private async Task RemoveRecipeFromCookbook(SavedRecipe recipe)
    {
        var confirmed = await Shell.Current.DisplayAlert("Remove",
            $"Remove \"{recipe.Name}\" from {Cookbook?.Name}?", "Remove", "Cancel");
        if (!confirmed) return;

        recipe.CookbookIds.Remove(_cookbookId);
        var success = await _cookbookService.UpdateRecipeCookbooksAsync(recipe.RowId, recipe.CookbookIds);
        if (success)
        {
            Recipes.Remove(recipe);
            if (Cookbook != null) Cookbook.RecipeCount = Recipes.Count;
        }
    }

    private async Task MoveRecipeToCookbook(SavedRecipe recipe)
    {
        var allCookbooks = await _cookbookService.GetCookbooksAsync();
        var targetNames = allCookbooks
            .Where(c => c.RowId != _cookbookId)
            .Select(c => c.Name)
            .ToArray();

        if (targetNames.Length == 0)
        {
            await Shell.Current.DisplayAlert("Info", "No other cookbooks exist.", "OK");
            return;
        }

        var selected = await Shell.Current.DisplayActionSheet("Move to...", "Cancel", null, targetNames);
        if (selected == null || selected == "Cancel") return;

        var target = allCookbooks.First(c => c.Name == selected);

        recipe.CookbookIds.Remove(_cookbookId);
        if (!recipe.CookbookIds.Contains(target.RowId))
            recipe.CookbookIds.Add(target.RowId);

        var success = await _cookbookService.UpdateRecipeCookbooksAsync(recipe.RowId, recipe.CookbookIds);
        if (success)
        {
            Recipes.Remove(recipe);
            if (Cookbook != null) Cookbook.RecipeCount = Recipes.Count;
        }
    }

    [RelayCommand]
    private async Task RenameCookbook()
    {
        if (Cookbook == null) return;
        var newName = await Shell.Current.DisplayPromptAsync("Rename",
            "Enter new name:", "Rename", "Cancel", initialValue: Cookbook.Name);
        if (string.IsNullOrWhiteSpace(newName)) return;

        var success = await _cookbookService.RenameCookbookAsync(Cookbook.RowId, newName.Trim());
        if (success)
        {
            Cookbook.Name = newName.Trim();
            OnPropertyChanged(nameof(Cookbook));
        }
    }

    [RelayCommand]
    private async Task DeleteCookbook()
    {
        if (Cookbook == null) return;
        var confirmed = await Shell.Current.DisplayAlert("Delete",
            $"Delete \"{Cookbook.Name}\"? Recipes will be kept.", "Delete", "Cancel");
        if (!confirmed) return;

        var success = await _cookbookService.DeleteCookbookAsync(Cookbook.RowId);
        if (success)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add FridgeScan/ViewModels/CookbookDetailViewModel.cs
git commit -m "feat: add CookbookDetailViewModel with recipe list and edit actions"
```

---

### Task 7: Create CookbookDetailPage

**Files:**
- Create: `FridgeScan/Views/CookbookDetailPage.xaml`
- Create: `FridgeScan/Views/CookbookDetailPage.xaml.cs`

- [ ] **Step 1: Create CookbookDetailPage.xaml**

```xml
<ContentPage
    x:Class="FridgeScan.Views.CookbookDetailPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:sfCards="clr-namespace:Syncfusion.Maui.Cards;assembly=Syncfusion.Maui.Cards"
    xmlns:sfBusy="clr-namespace:Syncfusion.Maui.Core;assembly=Syncfusion.Maui.Core"
    xmlns:models="clr-namespace:FridgeScan.Models"
    xmlns:viewmodel="clr-namespace:FridgeScan.ViewModels"
    Title="{Binding Cookbook.Name}">

    <Grid RowDefinitions="Auto,Auto,*">

        <!-- Header -->
        <VerticalStackLayout Grid.Row="0" Padding="16,12" Spacing="4">
            <Label Text="{Binding Cookbook.Name}" FontSize="22" FontAttributes="Bold" />
            <Label FontSize="14" TextColor="Gray">
                <Label.Text>
                    <MultiBinding StringFormat="{}{0} recipes">
                        <Binding Path="Cookbook.RecipeCount" />
                    </MultiBinding>
                </Label.Text>
            </Label>
            <HorizontalStackLayout Spacing="8" Margin="0,8,0,0">
                <Button Text="Rename"
                        FontSize="13"
                        BackgroundColor="Transparent"
                        BorderColor="#555"
                        BorderWidth="1"
                        CornerRadius="6"
                        Padding="12,6"
                        TextColor="White"
                        Command="{Binding RenameCookbookCommand}" />
                <Button Text="Delete"
                        FontSize="13"
                        BackgroundColor="Transparent"
                        BorderColor="#663333"
                        BorderWidth="1"
                        CornerRadius="6"
                        Padding="12,6"
                        TextColor="#ff6b6b"
                        Command="{Binding DeleteCookbookCommand}" />
            </HorizontalStackLayout>
        </VerticalStackLayout>

        <BoxView Grid.Row="1" HeightRequest="1" Color="#333" Margin="16,0" />

        <!-- Recipe list -->
        <CollectionView Grid.Row="2"
                         ItemsSource="{Binding Recipes}"
                         SelectionMode="None"
                         Margin="8,8,8,0">
            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="models:SavedRecipe">
                    <sfCards:SfCardView Margin="4,4" CornerRadius="10" Padding="0">
                        <sfCards:SfCardView.GestureRecognizers>
                            <TapGestureRecognizer
                                Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodel:CookbookDetailViewModel}}, Path=OpenRecipeCommand}"
                                CommandParameter="{Binding}" />
                        </sfCards:SfCardView.GestureRecognizers>
                        <Grid ColumnDefinitions="110,*" HeightRequest="100">
                            <!-- Recipe image -->
                            <Image Grid.Column="0"
                                   Source="{Binding ImageUrl}"
                                   Aspect="AspectFill"
                                   WidthRequest="110"
                                   HeightRequest="100" />
                            <!-- Recipe info -->
                            <Grid Grid.Column="1"
                                  ColumnDefinitions="*,Auto"
                                  Padding="12,10">
                                <VerticalStackLayout VerticalOptions="Center" Spacing="2">
                                    <Label Text="{Binding Name}"
                                           FontSize="15"
                                           FontAttributes="Bold"
                                           LineBreakMode="TailTruncation"
                                           MaxLines="2" />
                                    <Label Text="{Binding RecipeSource}"
                                           FontSize="12"
                                           TextColor="Gray" />
                                    <HorizontalStackLayout Spacing="12">
                                        <Label Text="{Binding TotalTime}"
                                               FontSize="12"
                                               TextColor="Gray" />
                                        <Label Text="{Binding Difficulty}"
                                               FontSize="12"
                                               TextColor="Gray" />
                                    </HorizontalStackLayout>
                                </VerticalStackLayout>
                                <!-- Edit button -->
                                <Button Grid.Column="1"
                                        Text="&#x22ef;"
                                        FontSize="20"
                                        BackgroundColor="Transparent"
                                        TextColor="Gray"
                                        Padding="4"
                                        VerticalOptions="Center"
                                        Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodel:CookbookDetailViewModel}}, Path=EditRecipeCommand}"
                                        CommandParameter="{Binding}" />
                            </Grid>
                        </Grid>
                    </sfCards:SfCardView>
                </DataTemplate>
            </CollectionView.ItemTemplate>

            <CollectionView.EmptyView>
                <VerticalStackLayout HorizontalOptions="Center" VerticalOptions="Center" Padding="0,60" Spacing="8">
                    <Label Text="No recipes yet" FontSize="16" TextColor="Gray" HorizontalOptions="Center" />
                    <Label Text="Import a recipe to get started" FontSize="13" TextColor="Gray" HorizontalOptions="Center" />
                </VerticalStackLayout>
            </CollectionView.EmptyView>
        </CollectionView>

        <!-- Loading -->
        <sfBusy:SfBusyIndicator Grid.Row="2"
                                 AnimationType="CircularMaterial"
                                 IsRunning="{Binding IsLoading}"
                                 IsVisible="{Binding IsLoading}"
                                 HorizontalOptions="Center"
                                 VerticalOptions="Center"
                                 HeightRequest="60"
                                 WidthRequest="60" />
    </Grid>
</ContentPage>
```

- [ ] **Step 2: Create CookbookDetailPage.xaml.cs**

```csharp
using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class CookbookDetailPage : ContentPage
{
    public CookbookDetailPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<CookbookDetailViewModel>();
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add FridgeScan/Views/CookbookDetailPage.xaml FridgeScan/Views/CookbookDetailPage.xaml.cs
git commit -m "feat: add CookbookDetailPage with horizontal recipe cards"
```

---

### Task 8: Create RecipePreviewViewModel

**Files:**
- Create: `FridgeScan/ViewModels/RecipePreviewViewModel.cs`

- [ ] **Step 1: Create RecipePreviewViewModel**

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FridgeScan.Models;
using FridgeScan.Services;

namespace FridgeScan.ViewModels;

public partial class RecipePreviewViewModel : BaseViewModel, IQueryAttributable
{
    private readonly CookbookService _cookbookService;

    [ObservableProperty]
    private SavedRecipe? recipe;

    [ObservableProperty]
    private ObservableCollection<Cookbook> allCookbooks = new();

    [ObservableProperty]
    private ObservableCollection<Cookbook> selectedCookbooks = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isSaving;

    public RecipePreviewViewModel(CookbookService cookbookService)
    {
        _cookbookService = cookbookService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Recipe = new SavedRecipe
        {
            Name = GetString(query, "Name"),
            Url = GetString(query, "Url"),
            ImageUrl = GetString(query, "ImageUrl"),
            ImageUrlBig = GetString(query, "ImageUrl"),
            Description = GetString(query, "Description"),
            Difficulty = GetString(query, "Difficulty"),
            TotalTime = GetString(query, "TotalTime"),
            RecipeSource = GetString(query, "RecipeSource"),
            Ingredients = GetStringList(query, "Ingredients"),
            MethodSteps = GetStringList(query, "MethodSteps")
        };
        _ = LoadCookbooksAsync();
    }

    private async Task LoadCookbooksAsync()
    {
        try
        {
            IsLoading = true;
            var cookbooks = await _cookbookService.GetCookbooksAsync();
            AllCookbooks = new ObservableCollection<Cookbook>(cookbooks);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleCookbookSelection(Cookbook cookbook)
    {
        if (SelectedCookbooks.Contains(cookbook))
            SelectedCookbooks.Remove(cookbook);
        else
            SelectedCookbooks.Add(cookbook);
    }

    [RelayCommand]
    private async Task CreateAndAddCookbook()
    {
        var name = await Shell.Current.DisplayPromptAsync("New Cookbook",
            "Enter a name:", "Create", "Cancel");
        if (string.IsNullOrWhiteSpace(name)) return;

        var created = await _cookbookService.CreateCookbookAsync(name.Trim());
        if (created != null)
        {
            AllCookbooks.Add(created);
            SelectedCookbooks.Add(created);
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (Recipe == null || SelectedCookbooks.Count == 0) return;

        try
        {
            IsSaving = true;
            Recipe.CookbookIds = SelectedCookbooks.Select(c => c.RowId).ToList();

            var saved = await _cookbookService.SaveRecipeAsync(Recipe);
            if (saved != null)
            {
                // Navigate back to cookbooks tab
                await Shell.Current.GoToAsync("../..");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", "Failed to save recipe.", "OK");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task Close()
    {
        await Shell.Current.GoToAsync("..");
    }

    private static string? GetString(IDictionary<string, object> query, string key)
    {
        return query.TryGetValue(key, out var val) ? val?.ToString() : null;
    }

    private static List<string> GetStringList(IDictionary<string, object> query, string key)
    {
        if (query.TryGetValue(key, out var val) && val is List<string> list)
            return list;
        return new List<string>();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add FridgeScan/ViewModels/RecipePreviewViewModel.cs
git commit -m "feat: add RecipePreviewViewModel for import review and save to cookbook"
```

---

### Task 9: Create RecipePreviewPage

**Files:**
- Create: `FridgeScan/Views/RecipePreviewPage.xaml`
- Create: `FridgeScan/Views/RecipePreviewPage.xaml.cs`

- [ ] **Step 1: Create RecipePreviewPage.xaml**

```xml
<ContentPage
    x:Class="FridgeScan.Views.RecipePreviewPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:sfBusy="clr-namespace:Syncfusion.Maui.Core;assembly=Syncfusion.Maui.Core"
    xmlns:models="clr-namespace:FridgeScan.Models"
    xmlns:viewmodel="clr-namespace:FridgeScan.ViewModels"
    Title="Imported Recipe">

    <Grid RowDefinitions="*,Auto">

        <!-- Scrollable recipe content -->
        <ScrollView Grid.Row="0">
            <VerticalStackLayout Spacing="0" Padding="0">

                <!-- Image -->
                <Image Source="{Binding Recipe.ImageUrl}"
                       Aspect="AspectFill"
                       HeightRequest="200" />

                <!-- Details -->
                <VerticalStackLayout Padding="16" Spacing="8">
                    <Label Text="{Binding Recipe.Name}"
                           FontSize="22"
                           FontAttributes="Bold" />
                    <HorizontalStackLayout Spacing="12">
                        <Label Text="{Binding Recipe.RecipeSource}"
                               FontSize="13"
                               TextColor="Gray" />
                        <Label Text="{Binding Recipe.TotalTime}"
                               FontSize="13"
                               TextColor="Gray" />
                        <Label Text="{Binding Recipe.Difficulty}"
                               FontSize="13"
                               TextColor="Gray" />
                    </HorizontalStackLayout>

                    <Label Text="{Binding Recipe.Description}"
                           FontSize="14"
                           LineBreakMode="WordWrap"
                           Margin="0,4,0,0" />

                    <!-- Ingredients -->
                    <Label Text="Ingredients"
                           FontSize="16"
                           FontAttributes="Bold"
                           Margin="0,12,0,6" />
                    <Frame BackgroundColor="#252525"
                           BorderColor="Transparent"
                           CornerRadius="10"
                           Padding="0"
                           HasShadow="False">
                        <CollectionView ItemsSource="{Binding Recipe.Ingredients}"
                                         SelectionMode="None">
                            <CollectionView.ItemTemplate>
                                <DataTemplate x:DataType="x:String">
                                    <Grid Padding="14,10" ColumnDefinitions="Auto,*">
                                        <Label Grid.Column="1"
                                               Text="{Binding .}"
                                               FontSize="14" />
                                    </Grid>
                                </DataTemplate>
                            </CollectionView.ItemTemplate>
                        </CollectionView>
                    </Frame>

                    <!-- Method -->
                    <Label Text="Method"
                           FontSize="16"
                           FontAttributes="Bold"
                           Margin="0,12,0,6" />
                    <Frame BackgroundColor="#252525"
                           BorderColor="Transparent"
                           CornerRadius="10"
                           Padding="0"
                           HasShadow="False">
                        <CollectionView ItemsSource="{Binding Recipe.MethodSteps}"
                                         SelectionMode="None">
                            <CollectionView.ItemTemplate>
                                <DataTemplate x:DataType="x:String">
                                    <Grid Padding="14,8" ColumnDefinitions="Auto,*" ColumnSpacing="10">
                                        <Label Grid.Column="1"
                                               Text="{Binding .}"
                                               FontSize="14"
                                               LineBreakMode="WordWrap" />
                                    </Grid>
                                </DataTemplate>
                            </CollectionView.ItemTemplate>
                        </CollectionView>
                    </Frame>
                </VerticalStackLayout>
            </VerticalStackLayout>
        </ScrollView>

        <!-- Bottom save panel -->
        <Frame Grid.Row="1"
               BackgroundColor="#1e1e1e"
               BorderColor="#333"
               CornerRadius="0"
               Padding="16"
               HasShadow="False">
            <VerticalStackLayout Spacing="12">
                <Label Text="Save to Cookbook"
                       FontSize="15"
                       FontAttributes="Bold" />

                <!-- Cookbook chips -->
                <CollectionView ItemsSource="{Binding AllCookbooks}"
                                 SelectionMode="None"
                                 HeightRequest="44">
                    <CollectionView.ItemsLayout>
                        <LinearItemsLayout Orientation="Horizontal" ItemSpacing="8" />
                    </CollectionView.ItemsLayout>
                    <CollectionView.ItemTemplate>
                        <DataTemplate x:DataType="models:Cookbook">
                            <Frame Padding="10,8"
                                   CornerRadius="20"
                                   BorderColor="#555"
                                   BackgroundColor="Transparent"
                                   HasShadow="False">
                                <Frame.GestureRecognizers>
                                    <TapGestureRecognizer
                                        Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodel:RecipePreviewViewModel}}, Path=ToggleCookbookSelectionCommand}"
                                        CommandParameter="{Binding}" />
                                </Frame.GestureRecognizers>
                                <Label Text="{Binding Name}" FontSize="13" />
                            </Frame>
                        </DataTemplate>
                    </CollectionView.ItemTemplate>
                </CollectionView>

                <!-- + New Cookbook -->
                <Frame Padding="10,8"
                       CornerRadius="20"
                       BorderColor="#555"
                       BackgroundColor="Transparent"
                       HasShadow="False"
                       HorizontalOptions="Start">
                    <Frame.GestureRecognizers>
                        <TapGestureRecognizer Command="{Binding CreateAndAddCookbookCommand}" />
                    </Frame.GestureRecognizers>
                    <Label Text="+ New Cookbook" FontSize="13" TextColor="Gray" />
                </Frame>

                <!-- Save button -->
                <Button Text="Save Recipe"
                        BackgroundColor="#4a90d9"
                        TextColor="White"
                        CornerRadius="8"
                        HeightRequest="48"
                        FontSize="15"
                        Command="{Binding SaveCommand}"
                        />
            </VerticalStackLayout>
        </Frame>

        <!-- Loading overlay -->
        <sfBusy:SfBusyIndicator Grid.RowSpan="2"
                                 AnimationType="CircularMaterial"
                                 IsRunning="{Binding IsSaving}"
                                 IsVisible="{Binding IsSaving}"
                                 HorizontalOptions="Center"
                                 VerticalOptions="Center"
                                 HeightRequest="60"
                                 WidthRequest="60" />
    </Grid>
</ContentPage>
```

- [ ] **Step 2: Create RecipePreviewPage.xaml.cs**

```csharp
using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class RecipePreviewPage : ContentPage
{
    public RecipePreviewPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<RecipePreviewViewModel>();
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add FridgeScan/Views/RecipePreviewPage.xaml FridgeScan/Views/RecipePreviewPage.xaml.cs
git commit -m "feat: add RecipePreviewPage with review and save-to-cookbook panel"
```

---

### Task 10: Create SavedRecipeDetailViewModel

**Files:**
- Create: `FridgeScan/ViewModels/SavedRecipeDetailViewModel.cs`

- [ ] **Step 1: Create SavedRecipeDetailViewModel**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FridgeScan.Models;
using FridgeScan.Services;

namespace FridgeScan.ViewModels;

public partial class SavedRecipeDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly CookbookService _cookbookService;

    [ObservableProperty]
    private SavedRecipe? recipe;

    [ObservableProperty]
    private bool isLoading;

    public SavedRecipeDetailViewModel(CookbookService cookbookService)
    {
        _cookbookService = cookbookService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("RecipeId", out var id))
        {
            _ = LoadRecipeAsync(id?.ToString() ?? string.Empty);
        }
    }

    private async Task LoadRecipeAsync(string recipeId)
    {
        if (string.IsNullOrEmpty(recipeId)) return;
        try
        {
            IsLoading = true;
            Recipe = await _cookbookService.GetRecipeByIdAsync(recipeId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RemoveFromCookbook()
    {
        if (Recipe == null || Recipe.CookbookIds.Count == 0) return;

        var allCookbooks = await _cookbookService.GetCookbooksAsync();
        var relevantCookbooks = allCookbooks
            .Where(c => Recipe.CookbookIds.Contains(c.RowId))
            .Select(c => c.Name)
            .ToArray();

        var selected = await Shell.Current.DisplayActionSheet(
            "Remove from...", "Cancel", null, relevantCookbooks);

        if (selected == null || selected == "Cancel") return;

        var target = allCookbooks.First(c => c.Name == selected);
        Recipe.CookbookIds.Remove(target.RowId);

        var success = await _cookbookService.UpdateRecipeCookbooksAsync(Recipe.RowId, Recipe.CookbookIds);
        if (success && Recipe.CookbookIds.Count == 0)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    private async Task AddToCookbook()
    {
        if (Recipe == null) return;

        var allCookbooks = await _cookbookService.GetCookbooksAsync();
        var available = allCookbooks
            .Where(c => !Recipe.CookbookIds.Contains(c.RowId))
            .ToArray();

        if (available.Length == 0)
        {
            await Shell.Current.DisplayAlert("Info", "Recipe is already in all cookbooks.", "OK");
            return;
        }

        var selected = await Shell.Current.DisplayActionSheet(
            "Add to...", "Cancel", null, available.Select(c => c.Name).ToArray());

        if (selected == null || selected == "Cancel") return;

        var target = available.First(c => c.Name == selected);
        Recipe.CookbookIds.Add(target.RowId);
        await _cookbookService.UpdateRecipeCookbooksAsync(Recipe.RowId, Recipe.CookbookIds);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add FridgeScan/ViewModels/SavedRecipeDetailViewModel.cs
git commit -m "feat: add SavedRecipeDetailViewModel for viewing saved recipe details"
```

---

### Task 11: Create SavedRecipeDetailPage

**Files:**
- Create: `FridgeScan/Views/SavedRecipeDetailPage.xaml`
- Create: `FridgeScan/Views/SavedRecipeDetailPage.xaml.cs`

- [ ] **Step 1: Create SavedRecipeDetailPage.xaml**

```xml
<ContentPage
    x:Class="FridgeScan.Views.SavedRecipeDetailPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:sfBusy="clr-namespace:Syncfusion.Maui.Core;assembly=Syncfusion.Maui.Core"
    Title="{Binding Recipe.Name}">

    <Shell.TitleView>
        <Grid ColumnDefinitions="*,Auto,Auto" Padding="0,0,8,0" ColumnSpacing="4">
            <Label Grid.Column="0"
                   Text="{Binding Recipe.Name}"
                   FontSize="18"
                   FontAttributes="Bold"
                   VerticalOptions="Center"
                   LineBreakMode="TailTruncation" />
            <Label Grid.Column="1"
                   Text="+"
                   FontSize="22"
                   TextColor="#4a90d9"
                   VerticalOptions="Center"
                   Padding="8,0">
                <Label.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding AddToCookbookCommand}" />
                </Label.GestureRecognizers>
            </Label>
            <Label Grid.Column="2"
                   Text="&#x22ef;"
                   FontSize="20"
                   TextColor="Gray"
                   VerticalOptions="Center"
                   Padding="8,0">
                <Label.GestureRecognizers>
                    <TapGestureRecognizer Command="{Binding RemoveFromCookbookCommand}" />
                </Label.GestureRecognizers>
            </Label>
        </Grid>
    </Shell.TitleView>

    <Grid>
        <ScrollView>
            <VerticalStackLayout Spacing="0">
                <Image Source="{Binding Recipe.ImageUrlBig}"
                       Aspect="AspectFill"
                       HeightRequest="250" />

                <VerticalStackLayout Padding="16" Spacing="8">
                    <HorizontalStackLayout Spacing="12">
                        <Label Text="{Binding Recipe.RecipeSource}"
                               FontSize="13"
                               TextColor="Gray" />
                        <Label Text="{Binding Recipe.TotalTime}"
                               FontSize="13"
                               TextColor="Gray" />
                        <Label Text="{Binding Recipe.Difficulty}"
                               FontSize="13"
                               TextColor="Gray" />
                    </HorizontalStackLayout>

                    <Label Text="{Binding Recipe.Description}"
                           FontSize="14"
                           LineBreakMode="WordWrap" />

                    <!-- Ingredients -->
                    <Label Text="Ingredients"
                           FontSize="16"
                           FontAttributes="Bold"
                           Margin="0,12,0,6" />
                    <Frame BackgroundColor="#252525"
                           BorderColor="Transparent"
                           CornerRadius="10"
                           Padding="0"
                           HasShadow="False">
                        <CollectionView ItemsSource="{Binding Recipe.Ingredients}"
                                         SelectionMode="None">
                            <CollectionView.ItemTemplate>
                                <DataTemplate x:DataType="x:String">
                                    <Grid Padding="14,10" ColumnDefinitions="Auto,*">
                                        <Label Grid.Column="1"
                                               Text="{Binding .}"
                                               FontSize="14" />
                                    </Grid>
                                </DataTemplate>
                            </CollectionView.ItemTemplate>
                        </CollectionView>
                    </Frame>

                    <!-- Method -->
                    <Label Text="Method"
                           FontSize="16"
                           FontAttributes="Bold"
                           Margin="0,12,0,6" />
                    <Frame BackgroundColor="#252525"
                           BorderColor="Transparent"
                           CornerRadius="10"
                           Padding="0"
                           HasShadow="False">
                        <CollectionView ItemsSource="{Binding Recipe.MethodSteps}"
                                         SelectionMode="None">
                            <CollectionView.ItemTemplate>
                                <DataTemplate x:DataType="x:String">
                                    <Grid Padding="14,8" ColumnDefinitions="Auto,*" ColumnSpacing="10">
                                        <Label Grid.Column="1"
                                               Text="{Binding .}"
                                               FontSize="14"
                                               LineBreakMode="WordWrap" />
                                    </Grid>
                                </DataTemplate>
                            </CollectionView.ItemTemplate>
                        </CollectionView>
                    </Frame>
                </VerticalStackLayout>
            </VerticalStackLayout>
        </ScrollView>

        <sfBusy:SfBusyIndicator AnimationType="CircularMaterial"
                                 IsRunning="{Binding IsLoading}"
                                 IsVisible="{Binding IsLoading}"
                                 HorizontalOptions="Center"
                                 VerticalOptions="Center"
                                 HeightRequest="60"
                                 WidthRequest="60" />
    </Grid>
</ContentPage>
```

- [ ] **Step 2: Create SavedRecipeDetailPage.xaml.cs**

```csharp
using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class SavedRecipeDetailPage : ContentPage
{
    public SavedRecipeDetailPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<SavedRecipeDetailViewModel>();
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add FridgeScan/Views/SavedRecipeDetailPage.xaml FridgeScan/Views/SavedRecipeDetailPage.xaml.cs
git commit -m "feat: add SavedRecipeDetailPage for viewing saved recipe with full details"
```

---

### Task 12: Wire Up AppShell, MauiProgram.cs, and .csproj

**Files:**
- Modify: `FridgeScan/AppShell.xaml`
- Modify: `FridgeScan/AppShell.xaml.cs`
- Modify: `FridgeScan/MauiProgram.cs`
- Modify: `FridgeScan/FridgeScan.csproj`

- [ ] **Step 1: Add Cookbooks tab to AppShell.xaml**

Add after the Activities `</ShellContent>` closing tag, before `</TabBar>`:

```xml
<ShellContent
    Title="Cookbooks"
    ContentTemplate="{DataTemplate views:CookbookPage}">
    <ShellContent.Icon>
        <FontImageSource
            FontFamily="Material"
            Glyph="&#xe86d;" />
    </ShellContent.Icon>
</ShellContent>
```

- [ ] **Step 2: Register routes in AppShell.xaml.cs**

```csharp
public AppShell()
{
    InitializeComponent();

    Routing.RegisterRoute(nameof(RecipeDetailsPage), typeof(RecipeDetailsPage));
    Routing.RegisterRoute(nameof(SharedRecipePage), typeof(SharedRecipePage));
    Routing.RegisterRoute(nameof(CookbookDetailPage), typeof(CookbookDetailPage));
    Routing.RegisterRoute(nameof(RecipePreviewPage), typeof(RecipePreviewPage));
    Routing.RegisterRoute(nameof(SavedRecipeDetailPage), typeof(SavedRecipeDetailPage));
}
```

- [ ] **Step 3: Add DI registrations in MauiProgram.cs**

Add alongside existing ViewModel registrations:
```csharp
builder.Services.AddSingleton<CookbookViewModel>();
builder.Services.AddSingleton<CookbookDetailViewModel>();
builder.Services.AddSingleton<RecipePreviewViewModel>();
builder.Services.AddSingleton<SavedRecipeDetailViewModel>();
```

Add alongside existing service registrations:
```csharp
builder.Services.AddSingleton<CookbookService>();
```

Add alongside existing page registrations:
```csharp
builder.Services.AddTransient<Views.CookbookPage>();
builder.Services.AddTransient<Views.CookbookDetailPage>();
builder.Services.AddTransient<Views.RecipePreviewPage>();
builder.Services.AddTransient<Views.SavedRecipeDetailPage>();
```

- [ ] **Step 4: Add MauiXaml entries to .csproj**

Add inside the `<ItemGroup>` that already has the other MauiXaml entries:
```xml
<MauiXaml Update="Views\CookbookPage.xaml">
    <Generator>MSBuild:Compile</Generator>
</MauiXaml>
<MauiXaml Update="Views\CookbookDetailPage.xaml">
    <Generator>MSBuild:Compile</Generator>
</MauiXaml>
<MauiXaml Update="Views\RecipePreviewPage.xaml">
    <Generator>MSBuild:Compile</Generator>
</MauiXaml>
<MauiXaml Update="Views\SavedRecipeDetailPage.xaml">
    <Generator>MSBuild:Compile</Generator>
</MauiXaml>
```

Also add Compile entries in the other `<ItemGroup>` (alongside existing ones like `ActivitiesPage.xaml.cs`):
```xml
<Compile Update="Views\CookbookPage.xaml.cs">
    <DependentUpon>CookbookPage.xaml</DependentUpon>
</Compile>
<Compile Update="Views\CookbookDetailPage.xaml.cs">
    <DependentUpon>CookbookDetailPage.xaml</DependentUpon>
</Compile>
<Compile Update="Views\RecipePreviewPage.xaml.cs">
    <DependentUpon>RecipePreviewPage.xaml</DependentUpon>
</Compile>
<Compile Update="Views\SavedRecipeDetailPage.xaml.cs">
    <DependentUpon>SavedRecipeDetailPage.xaml</DependentUpon>
</Compile>
```

- [ ] **Step 5: Commit**

```bash
git add FridgeScan/AppShell.xaml FridgeScan/AppShell.xaml.cs FridgeScan/MauiProgram.cs FridgeScan/FridgeScan.csproj FridgeScan/Views/RecipePreviewPage.xaml
git commit -m "feat: wire up Cookbooks tab, routes, DI, and csproj entries"
```

---

### Task 13: Modify SharedRecipePage Flow

**Files:**
- Modify: `FridgeScan/ViewModels/SharedRecipeViewModel.cs`
- Modify: `FridgeScan/Views/SharedRecipePage.xaml`

- [ ] **Step 1: Add SaveToCookbook command to SharedRecipeViewModel**

Add after the `Close()` method:
```csharp
[RelayCommand]
private async Task SaveToCookbook()
{
    if (ImportedRecipe == null) return;

    var parameters = new Dictionary<string, object>
    {
        { "Name", ImportedRecipe.Name ?? string.Empty },
        { "Url", ImportedRecipe.Url ?? string.Empty },
        { "ImageUrl", ImportedRecipe.ImageUrl ?? string.Empty },
        { "Description", GetDescription(ImportedRecipe) },
        { "Difficulty", ImportedRecipe.Difficulty ?? string.Empty },
        { "TotalTime", ImportedRecipe.CookTime ?? ImportedRecipe.PrepTime ?? string.Empty },
        { "RecipeSource", ImportedRecipe.RecipeSource ?? string.Empty },
        { "Ingredients", ImportedRecipe.Ingredients },
        { "MethodSteps", ImportedRecipe.MethodSteps }
    };

    await Shell.Current.GoToAsync($"../{nameof(Views.RecipePreviewPage)}", parameters);
}

private static string GetDescription(RecipeSuggestion recipe)
{
    // Combine available info into a description
    var parts = new List<string>();
    if (!string.IsNullOrWhiteSpace(recipe.DishType))
        parts.Add(recipe.DishType);
    if (!string.IsNullOrWhiteSpace(recipe.Serving))
        parts.Add($"Serves {recipe.Serving}");
    return string.Join(" · ", parts);
}
```

- [ ] **Step 2: Update SharedRecipePage.xaml — replace Close button with stack**

Replace the single Close button at the bottom of the page:
```xml
<button:SfButton
    Command="{Binding CloseCommand}"
    CornerRadius="6"
    HeightRequest="44"
    HorizontalOptions="Center"
    Text="Close"
    WidthRequest="120" />
```

With two buttons:
```xml
<HorizontalStackLayout Spacing="12" HorizontalOptions="Center">
    <button:SfButton
        Command="{Binding CloseCommand}"
        CornerRadius="6"
        HeightRequest="44"
        HorizontalOptions="Center"
        Text="Close"
        WidthRequest="120" />
    <button:SfButton
        Command="{Binding SaveToCookbookCommand}"
        CornerRadius="6"
        HeightRequest="44"
        HorizontalOptions="Center"
        Text="Save to Cookbook"
        IsVisible="{Binding HasRecipe}"
        BackgroundColor="#4a90d9"
        TextColor="White"
        WidthRequest="160" />
</HorizontalStackLayout>
```

- [ ] **Step 3: Commit**

```bash
git add FridgeScan/ViewModels/SharedRecipeViewModel.cs FridgeScan/Views/SharedRecipePage.xaml
git commit -m "feat: add Save to Cookbook flow to SharedRecipePage"
```

---

### Task 14: Build Verification

- [ ] **Step 1: Build the project**

```bash
dotnet build FridgeScan/FridgeScan.csproj -f net9.0-android
```

- [ ] **Step 2: Fix any build errors**

Common issues to check:
- Missing `using` directives in ViewModels and pages
- XAML namespace prefixes matching the code-behind classes
- `IQueryAttributable` requires `using System.ComponentModel` (often implicit via global usings)
- The `CookbookMosaic` control needs correct namespace references in XAML

- [ ] **Step 3: Commit any fixes**

```bash
git add -A
git commit -m "fix: build fixes for cookbooks feature"
```
