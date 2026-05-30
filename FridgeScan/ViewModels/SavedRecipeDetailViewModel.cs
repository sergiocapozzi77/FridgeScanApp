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
