using Syncfusion.Maui.DataSource;
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

        // Set up native SfListView grouping after the view loads
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (listView.DataSource.GroupDescriptors.Count == 0)
        {
            listView.DataSource.GroupDescriptors.Add(new GroupDescriptor()
            {
                PropertyName = "Category"
            });
            listView.DataSource.LiveDataUpdateMode = LiveDataUpdateMode.AllowDataShaping;
        }
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
            await ((ProductsViewModel)BindingContext).LoadProductsAsync();
        }
        finally
        {
            pullToRefresh.IsRefreshing = false;
        }
    }

    private async void OnEditProductTapped(object sender, EventArgs e)
    {
        if (sender is Border border && border.BindingContext is Models.Product product)
        {
            var vm = (ProductsViewModel)BindingContext;
            if (vm.EditProductCommand.CanExecute(product))
                vm.EditProductCommand.Execute(product);
        }
    }
}
