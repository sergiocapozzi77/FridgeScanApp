using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FridgeScan.Models;
using FridgeScan.Services;

namespace FridgeScan.ViewModels;

public partial class CookbookViewModel : BaseViewModel
{
    private const string Tag = "FridgeScan.CookbookViewModel";

    private readonly CookbookService _cookbookService;
    private readonly FavouriteService _favouriteService;

    [ObservableProperty]
    private ObservableCollection<Cookbook> cookbooks = new();

    [ObservableProperty]
    private bool isLoading;

    public CookbookViewModel(CookbookService cookbookService, FavouriteService favouriteService)
    {
        _cookbookService = cookbookService;
        _favouriteService = favouriteService;
    }

    [RelayCommand]
    private async Task LoadCookbooks()
    {
        if (IsLoading) return;
        try
        {
            IsLoading = true;
            var allCookbooks = await _cookbookService.GetCookbooksAsync();
            var allFavourites = await _favouriteService.GetAllFavouritesAsync();

            foreach (var cookbook in allCookbooks)
            {
                var cookbookFavourites = allFavourites
                    .Where(r => r.CookbookIds.Contains(cookbook.RowId))
                    .ToList();
                cookbook.RecipeCount = cookbookFavourites.Count;
                cookbook.PreviewImageUrls = cookbookFavourites
                    .Select(r => r.ImageUrl)
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Take(4)
                    .ToList()!;
            }

            var list = new List<Cookbook>(allCookbooks);

            // Add virtual "Uncategorised" cookbook for orphan recipes
            var validIds = allCookbooks.Select(c => c.RowId).ToList();
            var orphanRecipes = await _favouriteService.GetOrphanFavouritesAsync(validIds);
            if (orphanRecipes.Count > 0)
            {
                list.Insert(0, new Cookbook
                {
                    RowId = Cookbook.UncategorisedId,
                    Name = "Uncategorised",
                    RecipeCount = orphanRecipes.Count,
                    PreviewImageUrls = orphanRecipes
                        .Select(r => r.ImageUrl)
                        .Where(url => !string.IsNullOrWhiteSpace(url))
                        .Take(4)
                        .ToList()!
                });
            }

            Cookbooks = new ObservableCollection<Cookbook>(list);
        }
        catch (Exception ex)
        {
            Logger.Error(Tag, $"Error loading cookbooks: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateCookbook()
    {
        var name = await Shell.Current.DisplayPromptAsync("New Cookbook", "Enter a name for the cookbook:", "Create", "Cancel");
        if (string.IsNullOrWhiteSpace(name)) return;

        var created = await _cookbookService.CreateCookbookAsync(name.Trim());
        if (created != null)
        {
            Cookbooks.Add(created);
        }
    }

    [RelayCommand]
    private async Task RenameCookbook(Cookbook cookbook)
    {
        if (cookbook.RowId == Cookbook.UncategorisedId) return;

        var newName = await Shell.Current.DisplayPromptAsync("Rename", "Enter new name:", "Rename", "Cancel",
            initialValue: cookbook.Name);
        if (string.IsNullOrWhiteSpace(newName)) return;

        var success = await _cookbookService.RenameCookbookAsync(cookbook.RowId, newName.Trim());
        if (success)
        {
            cookbook.Name = newName.Trim();
            var index = Cookbooks.IndexOf(cookbook);
            if (index >= 0)
            {
                Cookbooks.RemoveAt(index);
                Cookbooks.Insert(index, cookbook);
            }
        }
    }

    [RelayCommand]
    private async Task DeleteCookbook(Cookbook cookbook)
    {
        if (cookbook.RowId == Cookbook.UncategorisedId) return;

        var confirmed = await Shell.Current.DisplayAlert("Delete Cookbook",
            $"Delete \"{cookbook.Name}\"? Recipes will be kept but unlinked.", "Delete", "Cancel");
        if (!confirmed) return;

        var success = await _cookbookService.DeleteCookbookAsync(cookbook.RowId);
        if (success)
        {
            Cookbooks.Remove(cookbook);
        }
    }

    [RelayCommand]
    private async Task OpenCookbook(Cookbook cookbook)
    {
        var parameters = new Dictionary<string, object>
        {
            { "CookbookId", cookbook.RowId },
            { "CookbookName", cookbook.Name }
        };
        await Shell.Current.GoToAsync("CookbookDetailPage", parameters);
    }
}
