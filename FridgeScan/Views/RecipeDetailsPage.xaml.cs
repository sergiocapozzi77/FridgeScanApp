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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Impedisce allo schermo di spegnersi
        DeviceDisplay.Current.KeepScreenOn = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Ripristina il comportamento normale quando si esce dalla pagina
        DeviceDisplay.Current.KeepScreenOn = false;
    }
}