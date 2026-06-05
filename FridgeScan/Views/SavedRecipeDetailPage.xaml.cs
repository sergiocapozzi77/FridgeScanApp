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

        // Already loaded (back-navigation) — skip skeleton, show content immediately
        if (vm.Recipe != null)
        {
            RealContent.Opacity = 1;
            RealContent.InputTransparent = false;
            Skeleton.IsVisible = false;
            return;
        }

        // Subscribe to data loading completion
        vm.PropertyChanged += OnViewModelPropertyChanged;
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

        // Crossfade: skeleton fades out, real content fades in
        await Task.WhenAll(
            RealContent.FadeTo(1, 350, Easing.CubicOut),
            Skeleton.FadeTo(0, 300, Easing.CubicOut)
        );

        Skeleton.IsVisible = false;
        RealContent.InputTransparent = false;
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
