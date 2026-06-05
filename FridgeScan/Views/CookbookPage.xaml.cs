using FridgeScan.Models;
using FridgeScan.ViewModels;

namespace FridgeScan.Views;

public partial class CookbookPage : ContentPage
{
    private readonly CookbookViewModel _vm;
    private const string Tag = "FridgeScan.CookbookPage";

    public CookbookPage()
    {
        InitializeComponent();

        // Start invisible for fade-in entrance animation
        Content.Opacity = 0;

        var services = Application.Current?.Handler?.MauiContext?.Services;
        _vm = services.GetService<CookbookViewModel>()!;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_vm.Cookbooks.Count == 0)
            _vm.LoadCookbooksCommand.Execute(null);
        await PageAnimations.FadeIn(Content);
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
                Logger.Error(Tag, $"Error navigating to cookbook: {ex.Message}");
            }
        }
    }
}
