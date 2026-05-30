using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class CookbookPage : ContentPage
{
    private readonly CookbookViewModel _vm;

    public CookbookPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _vm = services.GetService<CookbookViewModel>()!;
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCookbooksCommand.Execute(null);
    }
}
