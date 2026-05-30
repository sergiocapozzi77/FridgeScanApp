using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class CookbookDetailPage : ContentPage
{
    public CookbookDetailPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<CookbookDetailViewModel>();
    }
}
