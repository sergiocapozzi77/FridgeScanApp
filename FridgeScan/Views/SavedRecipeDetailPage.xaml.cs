using System.ComponentModel;
using FridgeScan.Models;
using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class SavedRecipeDetailPage : ContentPage
{
    private SavedRecipeDetailViewModel? ViewModel => BindingContext as SavedRecipeDetailViewModel;

    public SavedRecipeDetailPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<SavedRecipeDetailViewModel>();

        CookbookPicker.Saved += OnCookbookPickerSaved;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var vm = ViewModel;
        if (vm == null) return;

        // Keep content invisible until ViewModel data finishes loading
        ContentPanel.Opacity = 0;

        vm.PropertyChanged += OnViewModelPropertyChanged;

        // Already loaded (back-navigation)
        if (vm.Recipe != null)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            await FadeInContentAsync();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        var vm = ViewModel;
        if (vm != null)
            vm.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SavedRecipeDetailViewModel.IsLoading)) return;

        var vm = ViewModel;
        if (vm == null || vm.IsLoading) return;

        vm.PropertyChanged -= OnViewModelPropertyChanged;
        await FadeInContentAsync();
    }

    private async Task FadeInContentAsync()
    {
        await Task.Delay(50);
        if (ContentPanel != null)
            await ContentPanel.FadeTo(1, 350, Easing.CubicOut);
    }

    private async void OnCookbookPickerSaved(object? sender, IList<string> selectedIds)
    {
        var vm = ViewModel;
        if (vm == null) return;
        await vm.SaveCookbookSelectionAsync(selectedIds);
    }

    private void OnIngredientTapped(object sender, EventArgs e)
    {
        if (sender is View view && view.BindingContext is IngredientItem item)
        {
            ViewModel?.ToggleIngredientCommand.Execute(item);
        }
    }

    private async void OnOverflowClicked(object sender, EventArgs e)
    {
        var vm = ViewModel;
        if (vm == null) return;

        var action = await DisplayActionSheet(null, "Cancel", null, "Delete recipe", "Add to cookbook");
        switch (action)
        {
            case "Delete recipe":
                vm.DeleteRecipeCommand.Execute(null);
                break;
            case "Add to cookbook":
                vm.ShowCookbookPickerCommand.Execute(null);
                break;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
