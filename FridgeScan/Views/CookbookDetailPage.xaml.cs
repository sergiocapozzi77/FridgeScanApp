using FridgeScan.Models;
using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class CookbookDetailPage : ContentPage
{
    private readonly CookbookDetailViewModel? _vm;

    public CookbookDetailPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _vm = services?.GetService<CookbookDetailViewModel>();
        BindingContext = _vm;
    }

    private async void OnBackClicked(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnRecipeSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is SavedRecipe recipe)
        {
            if (sender is CollectionView cv)
                cv.SelectedItem = null;

            if (_vm?.OpenRecipeCommand.CanExecute(recipe) == true)
                await _vm.OpenRecipeCommand.ExecuteAsync(recipe);
        }
    }
}
