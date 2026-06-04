using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FridgeScan.Models;
using FridgeScan.Services;
using FridgeScan.Services.RecipeImport;

namespace FridgeScan.ViewModels;

public partial class SavedRecipeDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly CookbookService _cookbookService;
    private readonly FavouriteService _favouriteService;
    private readonly Func<string, IRecipeService> _recipeServiceFactory;
    private readonly RecipeAiService _recipeAiService;

    public string TotalTimeDisplay =>
        !string.IsNullOrEmpty(Recipe?.TotalTime)
            ? Recipe.TotalTime
            : $"{Recipe?.PrepTime} + {Recipe?.CookTime}";

    [ObservableProperty]
    private SavedRecipe? recipe;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isSavedRecipe;

    // Cookbook picker state
    [ObservableProperty]
    private bool isCookbookPickerVisible;

    private IList<string> _preSelectedCookbookIds = new List<string>();
    public IList<string> PreSelectedCookbookIds => _preSelectedCookbookIds;

    // Computed visibility flags for conditional sections
    public bool HasDescription => !string.IsNullOrEmpty(Recipe?.Description);
    public bool HasServing => !string.IsNullOrEmpty(Recipe?.Serving);
    public bool HasPrepTime => !string.IsNullOrEmpty(Recipe?.PrepTime);
    public bool HasCookTime => !string.IsNullOrEmpty(Recipe?.CookTime);
    public bool HasTotalTime => !string.IsNullOrEmpty(Recipe?.TotalTime);
    public bool HasNutrition => Recipe?.Nutritions is { Count: > 0 };
    public bool HasMetadata => MetadataChips.Count > 0;
    public string RecipeSourceInitial =>
        string.IsNullOrEmpty(Recipe?.RecipeSource) ? "?" :
            Recipe.RecipeSource[0].ToString().ToUpper();
    public bool HasRecipeUrl => !string.IsNullOrEmpty(Recipe?.Url);
    public ObservableCollection<MetadataChip> MetadataChips { get; } = new();
    public ObservableCollection<IngredientItem> IngredientItems { get; } = new();
    public ObservableCollection<MethodStep> MethodSteps { get; } = new();

    [RelayCommand]
    private void ToggleIngredient(IngredientItem? item)
    {
        if (item == null) return;
        item.IsChecked = !item.IsChecked;
    }

    [RelayCommand]
    private async Task OpenRecipeUrl()
    {
        if (!string.IsNullOrEmpty(Recipe?.Url))
        {
            try
            {
                await Launcher.OpenAsync(new Uri(Recipe.Url));
            }
            catch
            {
                await Shell.Current.DisplayAlert("Error", "Could not open the recipe URL.", "OK");
            }
        }
    }

    [RelayCommand]
    private void ShowCookbookPicker()
    {
        _preSelectedCookbookIds = Recipe?.CookbookIds ?? new List<string>();
        OnPropertyChanged(nameof(PreSelectedCookbookIds));
        IsCookbookPickerVisible = true;
    }

    /// <summary>
    /// Called by the page after the CookbookPickerControl fires its Saved event.
    /// Persists the selected cookbook IDs and shows a toast confirmation.
    /// </summary>
    public async Task SaveCookbookSelectionAsync(IList<string> selectedIds)
    {
        if (Recipe == null) return;

        try
        {
            Recipe.CookbookIds = selectedIds.ToList();
            if (IsSavedRecipe && !string.IsNullOrEmpty(Recipe.RowId))
            {
                await _favouriteService.UpdateFavouriteCookbooksAsync(Recipe.RowId, Recipe.CookbookIds);
            }

            await Toast.Make("Cookbooks updated!").Show();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", "Failed to update cookbooks.", "OK");
        }
    }

    public SavedRecipeDetailViewModel(
        CookbookService cookbookService,
        FavouriteService favouriteService,
        Func<string, IRecipeService> recipeServiceFactory,
        RecipeAiService recipeAiService)
    {
        _cookbookService = cookbookService;
        _favouriteService = favouriteService;
        _recipeServiceFactory = recipeServiceFactory;
        _recipeAiService = recipeAiService;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // Case 1: Saved recipe — loaded by ID from FavouriteService
        if (query.TryGetValue("RecipeId", out var id))
        {
            IsSavedRecipe = true;
            _ = LoadRecipeAsync(id?.ToString() ?? string.Empty);
            return;
        }

        // Case 2: Web/AI recipe — loaded from scraping or AI service
        if (query.ContainsKey("RecipeUrl") && query.ContainsKey("provider") && query.ContainsKey("Recipe"))
        {
            IsSavedRecipe = false;
            var url = query["RecipeUrl"].ToString();
            var provider = query["provider"].ToString();
            var recipe = query["Recipe"] as RecipeSuggestion;

            await LoadRecipeDetails(provider, recipe);
        }
    }

    private async Task LoadRecipeAsync(string recipeId)
    {
        if (string.IsNullOrEmpty(recipeId)) return;
        try
        {
            IsLoading = true;
            Recipe = await _favouriteService.GetFavouriteByIdAsync(recipeId);
            NotifyVisibilityChanged();
            BuildIngredientItems();
            BuildMethodSteps();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadRecipeDetails(string? provider, RecipeSuggestion? suggestion)
    {
        if (provider == null || suggestion == null) return;

        IsLoading = true;

        try
        {
            RecipeSuggestion fullRecipe;
            if (provider == "AI")
            {
                fullRecipe = await _recipeAiService.GetFullRecipeDetailsAsync(suggestion);
            }
            else
            {
                var recipeService = _recipeServiceFactory(provider);
                fullRecipe = await recipeService.GetFullRecipeDetailsAsync(suggestion.Url);
            }

            // Map RecipeSuggestion → SavedRecipe for unified display
            Recipe = new SavedRecipe
            {
                Name = fullRecipe.Name,
                Url = fullRecipe.Url,
                ImageUrl = fullRecipe.ImageUrl,
                Difficulty = fullRecipe.Difficulty,
                RecipeSource = fullRecipe.RecipeSource,
                Serving = fullRecipe.Serving,
                PrepTime = fullRecipe.PrepTime,
                CookTime = fullRecipe.CookTime,
                Ingredients = fullRecipe.Ingredients,
                MethodSteps = fullRecipe.MethodSteps,
                Nutritions = fullRecipe.Nutritions,
                DishType = fullRecipe.DishType
            };
            NotifyVisibilityChanged();
            BuildIngredientItems();
            BuildMethodSteps();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NotifyVisibilityChanged()
    {
        OnPropertyChanged(nameof(HasDescription));
        OnPropertyChanged(nameof(HasServing));
        OnPropertyChanged(nameof(HasPrepTime));
        OnPropertyChanged(nameof(HasCookTime));
        OnPropertyChanged(nameof(HasTotalTime));
        OnPropertyChanged(nameof(HasNutrition));
        OnPropertyChanged(nameof(HasRecipeUrl));
        RebuildMetadataChips();
    }

    private void RebuildMetadataChips()
    {
        MetadataChips.Clear();
        if (Recipe == null) return;
        if (HasTotalTime)
            MetadataChips.Add(new MetadataChip { Icon = "", Value = Recipe.TotalTime, Label = "Total time" });
        else if (HasPrepTime && HasCookTime)
            MetadataChips.Add(new MetadataChip { Icon = "", Value = $"{Recipe.PrepTime} + {Recipe.CookTime}", Label = "Total time" });
        if (!string.IsNullOrEmpty(Recipe.Difficulty))
            MetadataChips.Add(new MetadataChip { Icon = "", Value = Recipe.Difficulty, Label = "Difficulty" });
        if (HasServing)
            MetadataChips.Add(new MetadataChip { Icon = "", Value = $"{Recipe.Serving} servings", Label = "Serves" });
        OnPropertyChanged(nameof(HasMetadata));
    }

    [RelayCommand]
    private async Task DeleteRecipe()
    {
        if (Recipe == null) return;

        var confirmed = await Shell.Current.DisplayAlert("Delete",
            $"Delete \"{Recipe.Name}\"?", "Delete", "Cancel");
        if (!confirmed) return;

        // For saved recipes, permanently delete from the database
        // For web/AI recipes, just navigate back
        if (IsSavedRecipe && !string.IsNullOrEmpty(Recipe.RowId))
        {
            var success = await _favouriteService.DeleteFavouriteAsync(Recipe.RowId);
            if (success)
            {
                await Shell.Current.GoToAsync("..");
            }
        }
        else
        {
            await Shell.Current.GoToAsync("..");
        }
    }

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
}
