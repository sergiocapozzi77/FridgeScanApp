using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FridgeScan.Models;
using FridgeScan.Services;
using FridgeScan.Services.RecipeImport;
using System.Text.RegularExpressions;

namespace FridgeScan.ViewModels;

public partial class SavedRecipeDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly CookbookService _cookbookService;
    private readonly FavouriteService _favouriteService;
    private readonly Func<string, IRecipeService> _recipeServiceFactory;
    private readonly RecipeAiService _recipeAiService;
    private readonly IRecipeIngredientParser _ingredientParser;

    private int _baseServings = 4;
    private readonly List<ParsedIngredient> _parsedIngredients = new();

    private static readonly Regex ServingNumberRegex = new(@"(\d+)", RegexOptions.Compiled);

    /// <summary>
    /// Extracts the first number from a serving string (e.g. "4", "4-6", "Serves 4").
    /// Returns 4 as default when no number is found.
    /// </summary>
    private static int ParseServingCount(string? serving)
    {
        if (string.IsNullOrWhiteSpace(serving))
            return 4;
        var match = ServingNumberRegex.Match(serving);
        return match.Success && int.TryParse(match.Groups[1].Value, out var count) ? count : 4;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ServingsLabel))]
    private int servingCount = 4;

    public string ServingsLabel => $"{ServingCount} servings";

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

    [RelayCommand]
    private void IncreaseServings()
    {
        var newServings = ServingCount + 1;
        var ratio = (float)newServings / _baseServings;
        ServingCount = newServings;
        RecalculateQuantities(ratio);
    }

    [RelayCommand]
    private void DecreaseServings()
    {
        if (ServingCount <= 1) return;
        var newServings = ServingCount - 1;
        var ratio = (float)newServings / _baseServings;
        ServingCount = newServings;
        RecalculateQuantities(ratio);
    }

    private void RecalculateQuantities(float ratio)
    {
        foreach (var item in IngredientItems)
        {
            item.AdjustQuantity(ratio);
        }
    }

    public SavedRecipeDetailViewModel(
        CookbookService cookbookService,
        FavouriteService favouriteService,
        Func<string, IRecipeService> recipeServiceFactory,
        RecipeAiService recipeAiService,
        IRecipeIngredientParser ingredientParser)
    {
        _cookbookService = cookbookService;
        _favouriteService = favouriteService;
        _recipeServiceFactory = recipeServiceFactory;
        _recipeAiService = recipeAiService;
        _ingredientParser = ingredientParser;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // Case 1: Pre-loaded recipe (prefetched by CookbookDetailViewModel)
        //          Content is ready immediately — no async loading needed.
        if (query.TryGetValue("Recipe", out var recipeObj) && recipeObj is SavedRecipe preloaded)
        {
            IsSavedRecipe = true;
            Recipe = preloaded;
            NotifyVisibilityChanged();
            BuildIngredientItems();
            BuildMethodSteps();
            return;
        }

        // Case 2: Saved recipe — loaded by ID from FavouriteService
        if (query.TryGetValue("RecipeId", out var id))
        {
            IsSavedRecipe = true;
            _ = LoadRecipeAsync(id?.ToString() ?? string.Empty);
            return;
        }

        // Case 3: Web/AI recipe — loaded from scraping or AI service
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
        _parsedIngredients.Clear();
        if (Recipe?.Ingredients == null) return;

        // Set base servings from recipe data (default 4)
        _baseServings = ParseServingCount(Recipe.Serving);
        ServingCount = _baseServings;

        var parsedList = _ingredientParser.Parse(Recipe.Ingredients);
        for (int i = 0; i < Recipe.Ingredients.Count; i++)
        {
            var raw = Recipe.Ingredients[i];
            var parsed = i < parsedList.Count ? parsedList[i] : null;

            _parsedIngredients.Add(parsed!);

            IngredientItems.Add(new IngredientItem(
                raw,
                parsed?.Quantity,
                parsed?.Unit,
                parsed?.Name));
        }
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
