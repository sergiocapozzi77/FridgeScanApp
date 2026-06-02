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

        Loaded += OnLoaded;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ProductsViewModel vm)
            vm.RefreshAfterEdit();
    }

    private void OnLoaded(object sender, EventArgs e)
    {
#if ANDROID
        ApplyCursorColor();
        if (addItemAutocomplete != null)
            addItemAutocomplete.Focused += OnAutocompleteFocused;
#endif
    }

#if ANDROID
    private void ApplyCursorColor()
    {
        SetCursorColorRecursive(addItemAutocomplete);
    }

    private void OnAutocompleteFocused(object sender, FocusEventArgs e)
    {
        SetCursorColorRecursive(addItemAutocomplete);
    }

    private static void SetCursorColorRecursive(Microsoft.Maui.Controls.View? element)
    {
        if (element?.Handler?.PlatformView is Android.Views.View view)
            SetCursorColor(view);
    }

    private static void SetCursorColor(Android.Views.View view)
    {
        if (view is Android.Widget.EditText editText)
        {
            var color = Android.Graphics.Color.ParseColor("#D0BCFF");
            var drawable = new Android.Graphics.Drawables.ColorDrawable(color);
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
            {
                editText.TextCursorDrawable = drawable;
            }
            return;
        }

        if (view is Android.Views.ViewGroup viewGroup)
        {
            for (int i = 0; i < viewGroup.ChildCount; i++)
            {
                SetCursorColor(viewGroup.GetChildAt(i));
            }
        }
    }
#endif

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

    private void OnSearchTapped(object sender, EventArgs e)
    {
        addItemAutocomplete?.Focus();
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
