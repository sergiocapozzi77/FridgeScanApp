namespace FridgeScan;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

        Routing.RegisterRoute(nameof(RecipeDetailsPage), typeof(RecipeDetailsPage));
    }
}
