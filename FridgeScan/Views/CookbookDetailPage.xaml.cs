using FridgeScan.Models;
using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class CookbookDetailPage : ContentPage
{
    private readonly CookbookDetailViewModel? _vm;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Reset any exit animation transform from back-navigation
        TranslationY = 0;
        Opacity = 1;
    }

    public CookbookDetailPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _vm = services?.GetService<CookbookDetailViewModel>();
        BindingContext = _vm;
    }

    private async void OnBackClicked(object? sender, TappedEventArgs e)
    {
        // Exit transition: slide down + fade out before popping
        await Task.WhenAll(
            this.TranslateTo(0, 60, 150, Easing.CubicIn),
            this.FadeTo(0, 150, Easing.CubicIn)
        );
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
