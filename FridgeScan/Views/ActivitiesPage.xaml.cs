namespace FridgeScan.Views;

public partial class ActivitiesPage : ContentPage
{
	public ActivitiesPage()
	{
		InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;

        var vm = services.GetService<ActivitiesViewModel>();
        BindingContext = vm;
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