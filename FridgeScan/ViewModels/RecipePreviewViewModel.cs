using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FridgeScan.Models;
using FridgeScan.Services;

namespace FridgeScan.ViewModels;

public partial class RecipePreviewViewModel : BaseViewModel, IQueryAttributable
{
    private readonly CookbookService _cookbookService;
    private readonly FavouriteService _favouriteService;

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

    public RecipePreviewViewModel(CookbookService cookbookService, FavouriteService favouriteService)
    {
        _cookbookService = cookbookService;
        _favouriteService = favouriteService;
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

            var saved = await _favouriteService.SaveFavouriteAsync(Recipe);
            if (saved != null)
            {
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
