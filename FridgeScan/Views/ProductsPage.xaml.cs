using Microsoft.Maui.Graphics;
using System;

namespace FridgeScan.Views;

public partial class ProductsPage : ContentPage
{
    public ProductsPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;

        var vm = services.GetService<ProductsViewModel>();
        BindingContext = vm;
    }

    private void SfAutocomplete_Completed(object sender, EventArgs e)
    {
        var autocomplete = sender as Syncfusion.Maui.Inputs.SfAutocomplete;
        if (autocomplete == null)
            return;

        // Call your ViewModel method
        ((ProductsViewModel)BindingContext).OnAddItem();

        // Dismiss the keyboard
        hiddenEntry.HideSoftInputAsync(CancellationToken.None);


    }

    private async void pullToRefresh_Refreshing(object sender, EventArgs e)
    {
        pullToRefresh.IsRefreshing = true;
        try
        {
            await((ProductsViewModel)BindingContext).LoadProductsAsync();
        }
        finally
        {
            pullToRefresh.IsRefreshing = false;
        }


    }
}
