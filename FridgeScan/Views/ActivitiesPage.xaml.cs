namespace FridgeScan.Views;

public partial class ActivitiesPage : ContentPage
{
	public ActivitiesPage()
	{
		InitializeComponent();

        // Start invisible for fade-in entrance animation
        Content.Opacity = 0;

        var services = Application.Current?.Handler?.MauiContext?.Services;

        var vm = services.GetService<ActivitiesViewModel>();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await PageAnimations.FadeIn(Content);
    }

    private async void pullToRefresh_Refreshing(object sender, EventArgs e)
    {
        pullToRefresh.IsRefreshing = true;
        try
        {
            await((ActivitiesViewModel)BindingContext).LoadActivitiesAsync();
        }
        finally
        {
            pullToRefresh.IsRefreshing = false;
        }
    }
}