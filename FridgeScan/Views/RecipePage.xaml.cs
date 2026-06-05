namespace FridgeScan.Views;

public partial class RecipePage : ContentPage
{
    public RecipePage()
    {
        InitializeComponent();

        // Start invisible for fade-in entrance animation
        Content.Opacity = 0;

        var services = Application.Current?.Handler?.MauiContext?.Services;

        var vm = services.GetService<RecipeViewModel>();
        BindingContext = vm;
        _viewModel = vm;
    }

    private readonly RecipeViewModel? _viewModel;

    private void Entry_Completed(object sender, EventArgs e)
    {
        var viewModel = this.BindingContext as RecipeViewModel;
        var name = (sender as InputView).Text;
        viewModel.Keywords.Add(name);
        (sender as InputView).Text = "";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel?.RefreshSelectedIngredients();
        await PageAnimations.FadeIn(Content);
    }
}
