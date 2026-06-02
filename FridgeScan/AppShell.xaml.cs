namespace FridgeScan;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

        Routing.RegisterRoute(nameof(SharedRecipePage), typeof(SharedRecipePage));
        Routing.RegisterRoute(nameof(CookbookDetailPage), typeof(CookbookDetailPage));
        Routing.RegisterRoute(nameof(RecipePreviewPage), typeof(RecipePreviewPage));
        Routing.RegisterRoute(nameof(SavedRecipeDetailPage), typeof(SavedRecipeDetailPage));
        Routing.RegisterRoute(nameof(ProductDetailPage), typeof(ProductDetailPage));
    }
}
