namespace FridgeScan.Views;

public partial class SharedRecipePage : ContentPage
{
    public SharedRecipePage()
    {
        InitializeComponent();

        // Present as modal on top of tabs
        Shell.SetPresentationMode(this, PresentationMode.ModalAnimated);

        var services = Application.Current?.Handler?.MauiContext?.Services;
        var vm = services.GetService<SharedRecipeViewModel>();
        BindingContext = vm;
    }
}
