namespace FridgeScan.Views;

public partial class RecipePage : ContentPage
{
    private readonly ProductsViewModel _vm;

    public RecipePage()
    {
        InitializeComponent();
        _vm = Application.Current?.Handler?.MauiContext?.Services?.GetService<ProductsViewModel>();
        BindingContext = _vm;
    }

    private void OnGenerateClicked(object sender, EventArgs e)
    {
        // Naive recipe generation: list product names as ingredients
        var ingredients = _vm.Products.Select(p => p.Name + (p.Quantity > 1 ? $" x{p.Quantity}" : string.Empty));
      //  RecipeLabel.Text = "Recipe suggestion:\n- " + string.Join("\n- ", ingredients);
    }
}
