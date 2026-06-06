using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FridgeScan.Models;
using FridgeScan.Services;

namespace FridgeScan.ViewModels;

public partial class CookbookDetailViewModel : BaseViewModel, IQueryAttributable
{
    private const string Tag = "FridgeScan.CookbookDetailViewModel";

    private readonly CookbookService _cookbookService;
    private readonly FavouriteService _favouriteService;

    [ObservableProperty]
    private Cookbook? cookbook;

    [ObservableProperty]
    private ObservableCollection<SavedRecipe> recipes = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isInitialLoading;

    public bool ShimmerActive => IsLoading || IsInitialLoading;

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(ShimmerActive));
    partial void OnIsInitialLoadingChanged(bool value) => OnPropertyChanged(nameof(ShimmerActive));

    [ObservableProperty]
    private bool isUncategorised;

    public bool HasActions => !IsUncategorised;

    private string _cookbookId = string.Empty;

    partial void OnIsUncategorisedChanged(bool value)
    {
        OnPropertyChanged(nameof(HasActions));
    }

    public CookbookDetailViewModel(CookbookService cookbookService, FavouriteService favouriteService)
    {
        _cookbookService = cookbookService;
        _favouriteService = favouriteService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // Track previous cookbook to detect fresh navigation vs back-navigation
        var previousCookbookId = _cookbookId;

        if (query.TryGetValue("CookbookId", out var id))
        {
            _cookbookId = id?.ToString() ?? string.Empty;
        }
        if (query.TryGetValue("CookbookName", out var name))
        {
            var cookbookName = name?.ToString() ?? string.Empty;
            IsUncategorised = _cookbookId == Cookbook.UncategorisedId;
            Cookbook = new Cookbook
            {
                RowId = _cookbookId,
                Name = IsUncategorised ? "Uncategorised" : cookbookName
            };
        }
        // Only load data on fresh navigation (new cookbook or first load).
        // Skipped when returning from a sub-page like SavedRecipeDetailPage.
        if (_cookbookId != previousCookbookId || Recipes.Count == 0)
        {
            Recipes.Clear();
            IsInitialLoading = true;
            _ = LoadRecipesWithInitialDelayAsync();
        }
    }

    private async Task LoadRecipesWithInitialDelayAsync()
    {
        await Task.Delay(400);
        await LoadRecipesAsync();
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

            List<SavedRecipe> list;
            if (_cookbookId == Cookbook.UncategorisedId)
            {
                var allCookbooks = await _cookbookService.GetCookbooksAsync();
                var validIds = allCookbooks.Select(c => c.RowId).ToList();
                list = await _favouriteService.GetOrphanFavouritesAsync(validIds);
            }
            else
            {
                list = await _favouriteService.GetFavouritesByCookbookIdAsync(_cookbookId);
            }

            Recipes = new ObservableCollection<SavedRecipe>(list);
            if (Cookbook != null)
            {
                Cookbook.RecipeCount = list.Count;
                OnPropertyChanged(nameof(Cookbook));
            }
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"Error loading recipes: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            IsInitialLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenRecipe(SavedRecipe recipe)
    {
        // Navigate immediately — SavedRecipeDetailPage handles loading + shimmer
        var parameters = new Dictionary<string, object>
        {
            { "RecipeId", recipe.RowId }
        };
        await Shell.Current.GoToAsync("SavedRecipeDetailPage", parameters);
    }

    [RelayCommand]
    private async Task EditRecipe(SavedRecipe recipe)
    {
        string action;
        if (IsUncategorised)
        {
            action = await Shell.Current.DisplayActionSheet(
                recipe.Name ?? "Recipe", "Cancel", null,
                "Add to cookbook");
        }
        else
        {
            action = await Shell.Current.DisplayActionSheet(
                recipe.Name ?? "Recipe", "Cancel", null,
                "Remove from cookbook", "Move to another cookbook");
        }

        switch (action)
        {
            case "Remove from cookbook":
                await RemoveRecipeFromCookbook(recipe);
                break;
            case "Move to another cookbook":
            case "Add to cookbook":
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
        var success = await _favouriteService.UpdateFavouriteCookbooksAsync(recipe.RowId, recipe.CookbookIds);
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

        var success = await _favouriteService.UpdateFavouriteCookbooksAsync(recipe.RowId, recipe.CookbookIds);
        if (success)
        {
            Recipes.Remove(recipe);
            if (Cookbook != null) Cookbook.RecipeCount = Recipes.Count;
        }
    }

    [RelayCommand]
    private async Task RenameCookbook()
    {
        if (IsUncategorised) return;
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
        if (IsUncategorised) return;
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
