using System.ComponentModel;
using FridgeScan.Models;
using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class CookbookDetailPage : ContentPage
{
    private readonly CookbookDetailViewModel? _vm;
    private const string Tag = "FridgeScan.CookbookDetailPage";

    public CookbookDetailPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _vm = services?.GetService<CookbookDetailViewModel>();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm == null) return;

        // Keep content invisible until ViewModel data finishes loading.
        // This prevents the abrupt "content pop" when recipes arrive
        // from Appwrite mid-transition.
        ContentArea.Opacity = 0;

        // Subscribe first (avoids race with fast data loads)
        _vm.PropertyChanged += OnViewModelPropertyChanged;

        // Already loaded (back-navigation from SavedRecipeDetailPage)
        if (_vm.Recipes.Count > 0)
        {
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
            await FadeInContentAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_vm != null)
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CookbookDetailViewModel.IsLoading)) return;
        if (_vm!.IsLoading) return;

        _vm.PropertyChanged -= OnViewModelPropertyChanged;
        await FadeInContentAsync();
    }

    private async Task FadeInContentAsync()
    {
        // Let the CollectionView render its items before the fade
        await Task.Delay(50);
        if (ContentArea != null)
            await ContentArea.FadeTo(1, 350, Easing.CubicOut);
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
