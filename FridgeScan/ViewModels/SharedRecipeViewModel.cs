using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using FridgeScan.Helpers;
using FridgeScan.Models;
using FridgeScan.Services;
using FridgeScan.Services.RecipeImport;

namespace FridgeScan.ViewModels;

public partial class SharedRecipeViewModel : BaseViewModel, IQueryAttributable
{
    private const string Tag = "FridgeScan.SharedRecipe";

    private readonly RecipeImportService _importService;
    private readonly CookbookService _cookbookService;
    private readonly FavouriteService _favouriteService;

    [ObservableProperty] private string sharedUrl = string.Empty;
    [ObservableProperty] private string pageTitle = "Importing...";
    [ObservableProperty] private bool isLoading = true;
    [ObservableProperty] private RecipeSuggestion? importedRecipe;
    [ObservableProperty] private bool hasRecipe;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private ObservableCollection<Cookbook> allCookbooks = new();
    [ObservableProperty] private ObservableCollection<Cookbook> selectedCookbooks = new();
    [ObservableProperty] private bool isCookbookPanelVisible;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private Cookbook? selectedCookbook;
    public ObservableCollection<MethodStep> DisplaySteps { get; } = new();

    partial void OnSelectedCookbookChanged(Cookbook? value)
    {
        if (value == null) return;

        value.IsSelected = !value.IsSelected;
        if (value.IsSelected)
        {
            SelectedCookbooks.Add(value);
            Logger.Debug(Tag, $"Cookbook selected: '{value.Name}' (id={value.RowId}), total selected={SelectedCookbooks.Count}");
        }
        else
        {
            SelectedCookbooks.Remove(value);
            Logger.Debug(Tag, $"Cookbook deselected: '{value.Name}' (id={value.RowId}), total selected={SelectedCookbooks.Count}");
        }

        SelectedCookbook = null;
    }

    public bool HasPrepTime => !string.IsNullOrWhiteSpace(ImportedRecipe?.PrepTime);
    public bool HasCookTime => !string.IsNullOrWhiteSpace(ImportedRecipe?.CookTime);
    public bool HasServing  => !string.IsNullOrWhiteSpace(ImportedRecipe?.Serving);
    public bool ShowDefaultButtons => !IsCookbookPanelVisible;

    partial void OnImportedRecipeChanged(RecipeSuggestion? value)
    {
        OnPropertyChanged(nameof(HasPrepTime));
        OnPropertyChanged(nameof(HasCookTime));
        OnPropertyChanged(nameof(HasServing));
        if (value != null)
            _ = LoadCookbooksAsync();
    }

    partial void OnIsCookbookPanelVisibleChanged(bool value) =>
        OnPropertyChanged(nameof(ShowDefaultButtons));

    public SharedRecipeViewModel(
        RecipeImportService importService,
        CookbookService cookbookService,
        FavouriteService favouriteService)
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
            Logger.Debug(Tag, $"ApplyQueryAttributes: url='{decoded}'");
            SharedUrl = decoded;
            IsLoading = true;
            HasError = false;
            PageTitle = "Importing...";
            _ = ImportRecipeAsync(decoded);
        }
        else
        {
            Logger.Error(Tag, "ApplyQueryAttributes: no 'url' key in query attributes");
            IsLoading = false;
            HasError = true;
            PageTitle = "Import Failed";
        }
    }

    private async Task ImportRecipeAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Logger.Error(Tag, "ImportRecipeAsync: called with empty URL, aborting");
            return;
        }

        Logger.Debug(Tag, $"ImportRecipeAsync: starting import for url='{url}'");
        IsLoading = true;
        HasRecipe = false;
        HasError = false;
        PageTitle = "Importing...";

        try
        {
            await Task.Delay(1000);
            
            ImportedRecipe = await _importService.ImportFromUrlAsync(url);

            if (ImportedRecipe != null)
            {
                HasRecipe = true;
                BuildDisplaySteps();
                PageTitle = ImportedRecipe.Name ?? "Imported Recipe";
                Logger.Debug(Tag, $"ImportRecipeAsync: success — name='{ImportedRecipe.Name}', source='{ImportedRecipe.RecipeSource}'");
            }
            else
            {
                HasError = true;
                PageTitle = "Import Failed";
                ErrorMessage = _importService.LastErrorMessage ?? "Could not import recipe from this URL.";
                Logger.Error(Tag, $"ImportRecipeAsync: service returned null — LastErrorMessage='{_importService.LastErrorMessage}'");
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            PageTitle = "Import Failed";
            ErrorMessage = "An unexpected error occurred while importing.";
            Logger.Error(Tag, "ImportRecipeAsync: unhandled exception", ex);
        }
        finally
        {
            IsLoading = false;
            Logger.Debug(Tag, $"ImportRecipeAsync: finished — HasRecipe={HasRecipe}, HasError={HasError}");
        }
    }

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

    private async Task LoadCookbooksAsync()
    {
        Logger.Debug(Tag, "LoadCookbooksAsync: loading cookbooks");
        try
        {
            var cookbooks = await _cookbookService.GetCookbooksAsync();
            AllCookbooks = new ObservableCollection<Cookbook>(cookbooks);
            Logger.Debug(Tag, $"LoadCookbooksAsync: loaded {AllCookbooks.Count} cookbooks");
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, "LoadCookbooksAsync: failed to load cookbooks", ex);
        }
    }

    [RelayCommand]
    private void ShowCookbookPanel()
    {
        Logger.Debug(Tag, "ShowCookbookPanel");
        IsCookbookPanelVisible = true;
    }

    [RelayCommand]
    private void HideCookbookPanel()
    {
        Logger.Debug(Tag, "HideCookbookPanel");
        IsCookbookPanelVisible = false;
    }

    [RelayCommand]
    private async Task CreateAndAddCookbook()
    {
        Logger.Debug(Tag, "CreateAndAddCookbook: prompting user");
        var name = await Shell.Current.DisplayPromptAsync("New Cookbook",
            "Enter a name:", "Create", "Cancel");

        if (string.IsNullOrWhiteSpace(name))
        {
            Logger.Debug(Tag, "CreateAndAddCookbook: cancelled or empty name");
            return;
        }

        Logger.Debug(Tag, $"CreateAndAddCookbook: creating '{name.Trim()}'");
        try
        {
            var created = await _cookbookService.CreateCookbookAsync(name.Trim());
            if (created != null)
            {
                AllCookbooks.Add(created);
                SelectedCookbooks.Add(created);
                Logger.Debug(Tag, $"CreateAndAddCookbook: created id={created.RowId}, name='{created.Name}'");
            }
            else
            {
                Logger.Error(Tag, $"CreateAndAddCookbook: service returned null for name='{name.Trim()}'");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"CreateAndAddCookbook: failed to create cookbook '{name.Trim()}'", ex);
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (ImportedRecipe == null)
        {
            Logger.Error(Tag, "Save: called with null ImportedRecipe, aborting");
            return;
        }

        if (SelectedCookbooks.Count == 0)
        {
            Logger.Debug(Tag, "Save: no cookbooks selected, showing alert");
            await Shell.Current.DisplayAlert("Select a Cookbook",
                "Please select at least one cookbook to save to.", "OK");
            return;
        }

        var cookbookIds = SelectedCookbooks.Select(c => c.RowId).ToList();
        Logger.Debug(Tag, $"Save: saving '{ImportedRecipe.Name}' to cookbooks [{string.Join(",", cookbookIds)}]");

        try
        {
            IsSaving = true;

            var recipe = new SavedRecipe
            {
                Name         = ImportedRecipe.Name         ?? string.Empty,
                Url          = ImportedRecipe.Url          ?? string.Empty,
                ImageUrl     = ImportedRecipe.ImageUrl     ?? string.Empty,
                ImageUrlBig  = ImportedRecipe.ImageUrl     ?? string.Empty,
                Description  = GetDescription(ImportedRecipe),
                Difficulty   = ImportedRecipe.Difficulty   ?? string.Empty,
                TotalTime    = ImportedRecipe.CookTime     ?? ImportedRecipe.PrepTime ?? string.Empty,
                RecipeSource = ImportedRecipe.RecipeSource ?? string.Empty,
                Ingredients  = ImportedRecipe.Ingredients,
                MethodSteps  = ImportedRecipe.MethodSteps,
                CookbookIds  = cookbookIds
            };

            var saved = await _favouriteService.SaveFavouriteAsync(recipe);
            if (saved != null)
            {
                Logger.Debug(Tag, $"Save: success — saved recipe id='{saved.RowId}', name='{saved.Name ?? "?"}'");
                var toast = Toast.Make("Recipe saved!");
                await toast.Show();
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                Logger.Error(Tag, $"Save: SaveFavouriteAsync returned null for recipe='{ImportedRecipe.Name}'");
                await Shell.Current.DisplayAlert("Error",
                    "Failed to save recipe. Check the debug log for details.", "OK");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"Save: unhandled exception for recipe='{ImportedRecipe.Name}'", ex);
            await Shell.Current.DisplayAlert("Error", "Failed to save recipe.", "OK");
        }
        finally
        {
            IsSaving = false;
            Logger.Debug(Tag, $"Save: finished — IsSaving={IsSaving}");
        }
    }

    [RelayCommand]
    private async Task Close()
    {
        Logger.Debug(Tag, "Close: navigating back");
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