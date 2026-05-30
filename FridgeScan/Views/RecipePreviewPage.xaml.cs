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
}
