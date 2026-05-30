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
}
