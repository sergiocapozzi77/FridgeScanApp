using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Maui.Alerts;
using FridgeScan.Models;
using FridgeScan.Services;
using FridgeScan.Services.RecipeImport;

namespace FridgeScan.ViewModels;

public partial class SharedRecipeViewModel : BaseViewModel, IQueryAttributable
{
    private readonly RecipeImportService _importService;
    private readonly CookbookService _cookbookService;
    private readonly FavouriteService _favouriteService;

    [ObservableProperty]
    private string sharedUrl = string.Empty;

    [ObservableProperty]
    private string pageTitle = "Importing...";

    [ObservableProperty]
    private bool isLoading = true;

    [ObservableProperty]
    private RecipeSuggestion? importedRecipe;

    [ObservableProperty]
    private bool hasRecipe;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private ObservableCollection<Cookbook> allCookbooks = new();

    [ObservableProperty]
    private ObservableCollection<Cookbook> selectedCookbooks = new();

    [ObservableProperty]
    private bool isCookbookPanelVisible;

    [ObservableProperty]
    private bool isSaving;

    [ObservableProperty]
    private Cookbook? selectedCookbook;

    partial void OnSelectedCookbookChanged(Cookbook? value)
    {
        if (value == null) return;

        value.IsSelected = !value.IsSelected;
        if (value.IsSelected)
            SelectedCookbooks.Add(value);
        else
            SelectedCookbooks.Remove(value);

        SelectedCookbook = null;
    }

    public bool HasPrepTime => !string.IsNullOrWhiteSpace(ImportedRecipe?.PrepTime);
    public bool HasCookTime => !string.IsNullOrWhiteSpace(ImportedRecipe?.CookTime);
    public bool HasServing => !string.IsNullOrWhiteSpace(ImportedRecipe?.Serving);
    public bool ShowDefaultButtons => !IsCookbookPanelVisible;

    partial void OnImportedRecipeChanged(RecipeSuggestion? value)
    {
        OnPropertyChanged(nameof(HasPrepTime));
        OnPropertyChanged(nameof(HasCookTime));
        OnPropertyChanged(nameof(HasServing));
        if (value != null)
            _ = LoadCookbooksAsync();
    }

    partial void OnIsCookbookPanelVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowDefaultButtons));
    }

    public SharedRecipeViewModel(RecipeImportService importService, CookbookService cookbookService, FavouriteService favouriteService)
    {
        _importService = importService;
        _cookbookService = cookbookService;
        _favouriteService = favouriteService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("url", out var url))
        {
            var decoded = Uri.UnescapeDataString(url?.ToString() ?? string.Empty);
            SharedUrl = decoded;
            IsLoading = true;
            HasError = false;
            PageTitle = "Importing...";
            _ = ImportRecipeAsync(decoded);
        }
        else
        {
            IsLoading = false;
            HasError = true;
            PageTitle = "Import Failed";
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

    private async Task LoadCookbooksAsync()
    {
        try
        {
            var cookbooks = await _cookbookService.GetCookbooksAsync();
            AllCookbooks = new ObservableCollection<Cookbook>(cookbooks);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading cookbooks: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowCookbookPanel()
    {
        IsCookbookPanelVisible = true;
    }

    [RelayCommand]
    private void HideCookbookPanel()
    {
        IsCookbookPanelVisible = false;
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
        if (ImportedRecipe == null) return;

        if (SelectedCookbooks.Count == 0)
        {
            await Shell.Current.DisplayAlert("Select a Cookbook", "Please select at least one cookbook to save to.", "OK");
            return;
        }

        try
        {
            IsSaving = true;

            var recipe = new SavedRecipe
            {
                Name = ImportedRecipe.Name ?? string.Empty,
                Url = ImportedRecipe.Url ?? string.Empty,
                ImageUrl = ImportedRecipe.ImageUrl ?? string.Empty,
                ImageUrlBig = ImportedRecipe.ImageUrl ?? string.Empty,
                Description = GetDescription(ImportedRecipe),
                Difficulty = ImportedRecipe.Difficulty ?? string.Empty,
                TotalTime = ImportedRecipe.CookTime ?? ImportedRecipe.PrepTime ?? string.Empty,
                RecipeSource = ImportedRecipe.RecipeSource ?? string.Empty,
                Ingredients = ImportedRecipe.Ingredients,
                MethodSteps = ImportedRecipe.MethodSteps,
                CookbookIds = SelectedCookbooks.Select(c => c.RowId).ToList()
            };

            var saved = await _favouriteService.SaveFavouriteAsync(recipe);
            if (saved != null)
            {
                var toast = Toast.Make("Recipe saved!");
                await toast.Show();
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to save recipe. Check the debug log for details.", "OK");
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
