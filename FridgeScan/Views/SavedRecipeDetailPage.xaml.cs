using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class SavedRecipeDetailPage : ContentPage
{
    public SavedRecipeDetailPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<SavedRecipeDetailViewModel>();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
