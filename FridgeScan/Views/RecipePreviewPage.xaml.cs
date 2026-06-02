using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class RecipePreviewPage : ContentPage
{
    public RecipePreviewPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<RecipePreviewViewModel>();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
