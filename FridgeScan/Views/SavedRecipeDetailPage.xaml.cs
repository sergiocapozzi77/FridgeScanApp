using FridgeScan.Models;
using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class SavedRecipeDetailPage : ContentPage
{

    private SavedRecipeDetailViewModel ViewModel => BindingContext as SavedRecipeDetailViewModel;

    public SavedRecipeDetailPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<SavedRecipeDetailViewModel>();
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
        var action = await DisplayActionSheet(null, "Cancel", null, "Delete recipe", "Add to cookbook");
        switch (action)
        {
            case "Delete recipe":
                ViewModel.DeleteRecipeCommand.Execute(null);
                break;
            case "Add to cookbook":
                ViewModel.AddToCookbookCommand.Execute(null);
                break;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
