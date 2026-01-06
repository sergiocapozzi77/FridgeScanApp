namespace FridgeScan.Views;

public partial class RecipeDetailsPage : ContentPage
{
	public RecipeDetailsPage()
	{
		InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;

        var vm = services.GetService<RecipeDetailsViewModel>();
        BindingContext = vm;
    }
}