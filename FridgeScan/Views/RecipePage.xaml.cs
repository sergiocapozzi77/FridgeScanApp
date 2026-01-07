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

    private void Entry_Completed(object sender, EventArgs e)
    {
        var viewModel = this.BindingContext as RecipeViewModel;
        var name = (sender as InputView).Text;
        viewModel.Keywords.Add(name);
        (sender as InputView).Text = "";
    }
}
