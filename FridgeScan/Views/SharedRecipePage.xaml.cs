namespace FridgeScan.Views;

public partial class SharedRecipePage : ContentPage
{
    public SharedRecipePage(SharedRecipeViewModel viewModel)
    {
        InitializeComponent();

        // Present as modal on top of tabs
        Shell.SetPresentationMode(this, PresentationMode.ModalAnimated);

        BindingContext = viewModel;
    }
}
