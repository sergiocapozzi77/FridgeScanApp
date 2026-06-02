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

    [ObservableProperty]
    private SavedRecipe? recipe;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isSavedRecipe;

    // Computed visibility flags for conditional sections
    public bool HasDescription => !string.IsNullOrEmpty(Recipe?.Description);
    public bool HasServing => !string.IsNullOrEmpty(Recipe?.Serving);
    public bool HasPrepTime => !string.IsNullOrEmpty(Recipe?.PrepTime);
    public bool HasCookTime => !string.IsNullOrEmpty(Recipe?.CookTime);
    public bool HasTotalTime => !string.IsNullOrEmpty(Recipe?.TotalTime);
    public bool HasNutrition => Recipe?.Nutritions is { Count: > 0 };
    public bool HasMetadata => MetadataChips.Count > 0;

    public ObservableCollection<string> MetadataChips { get; } = new();
    public ObservableCollection<IngredientItem> IngredientItems { get; } = new();
    public ObservableCollection<MethodStep> MethodSteps { get; } = new();

    [RelayCommand]
    private void ToggleIngredient(IngredientItem? item)
    {
        if (item == null) return;
        item.IsChecked = !item.IsChecked;
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
        RebuildMetadataChips();
    }

    private void RebuildMetadataChips()
    {
        MetadataChips.Clear();
        if (Recipe == null) return;

        if (HasServing)
            MetadataChips.Add($"Serves: {Recipe.Serving}");
        if (HasPrepTime)
            MetadataChips.Add($"Prep: {Recipe.PrepTime}");
        if (HasCookTime)
            MetadataChips.Add($"Cook: {Recipe.CookTime}");
        if (HasTotalTime)
            MetadataChips.Add($"Total: {Recipe.TotalTime}");
        if (!string.IsNullOrEmpty(Recipe.Difficulty))
            MetadataChips.Add(Recipe.Difficulty);

        OnPropertyChanged(nameof(HasMetadata));
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

        var success = await _favouriteService.UpdateFavouriteCookbooksAsync(Recipe.RowId, Recipe.CookbookIds);
        if (success && Recipe.CookbookIds.Count == 0)
        {
            await Shell.Current.GoToAsync("..");
        }
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
        await _favouriteService.UpdateFavouriteCookbooksAsync(Recipe.RowId, Recipe.CookbookIds);
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
        for (int i = 0; i < Recipe.MethodSteps.Count; i++)
            MethodSteps.Add(new MethodStep { Number = i + 1, Text = Recipe.MethodSteps[i] });
    }
}
