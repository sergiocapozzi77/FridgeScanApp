using FridgeScan.Models;
using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class CookbookPage : ContentPage
{
    private readonly CookbookViewModel _vm;

    public CookbookPage()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _vm = services.GetService<CookbookViewModel>()!;
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCookbooksCommand.Execute(null);
    }

    private async void OnCookbookSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Cookbook cookbook)
        {
            // Clear selection so tapping the same item works again
            if (sender is CollectionView cv)
                cv.SelectedItem = null;

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    { "CookbookId", cookbook.RowId },
                    { "CookbookName", cookbook.Name }
                };
                await Shell.Current.GoToAsync("CookbookDetailPage", parameters);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to cookbook: {ex.Message}");
            }
        }
    }
}
