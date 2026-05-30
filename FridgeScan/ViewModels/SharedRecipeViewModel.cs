using FridgeScan.Services.RecipeImport;

namespace FridgeScan.ViewModels;

public partial class SharedRecipeViewModel : BaseViewModel, IQueryAttributable
{
    private readonly RecipeImportService _importService;

    [ObservableProperty]
    private string sharedUrl = string.Empty;

    [ObservableProperty]
    private string pageTitle = "Import Recipe";

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private RecipeSuggestion? importedRecipe;

    [ObservableProperty]
    private bool hasRecipe;

    [ObservableProperty]
    private bool hasError;

    public SharedRecipeViewModel(RecipeImportService importService)
    {
        _importService = importService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("url", out var url))
        {
            var decoded = Uri.UnescapeDataString(url?.ToString() ?? string.Empty);
            SharedUrl = decoded;
            _ = ImportRecipeAsync(decoded);
        }
    }

    private async Task ImportRecipeAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        IsLoading = true;
        HasRecipe = false;
        HasError = false;
        PageTitle = "Importing...";

        try
        {
            ImportedRecipe = await _importService.ImportFromUrlAsync(url);

            if (ImportedRecipe != null)
            {
                HasRecipe = true;
                PageTitle = ImportedRecipe.Name ?? "Imported Recipe";
            }
            else
            {
                HasError = true;
                PageTitle = "Import Failed";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Import failed: {ex.Message}");
            HasError = true;
            PageTitle = "Import Failed";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Close()
    {
        await Shell.Current.GoToAsync("..");
    }

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

        await Shell.Current.GoToAsync($"../{"RecipePreviewPage"}", parameters);
    }

    private static string GetDescription(RecipeSuggestion recipe)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(recipe.DishType))
            parts.Add(recipe.DishType);
        if (!string.IsNullOrWhiteSpace(recipe.Serving))
            parts.Add($"Serves {recipe.Serving}");
        return string.Join(" \u00b7 ", parts);
    }
}
