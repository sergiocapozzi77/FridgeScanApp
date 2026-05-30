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
}
