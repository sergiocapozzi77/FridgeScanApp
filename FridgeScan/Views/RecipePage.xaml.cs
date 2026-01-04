namespace FridgeScan.Views;

public partial class RecipePage : ContentPage
{
    private readonly ProductsViewModel _vm;

    public RecipePage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;

        var vm = services.GetService<RecipeViewModel>();
        BindingContext = vm;
    }
}
