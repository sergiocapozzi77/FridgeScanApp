using Microsoft.Maui.Graphics;
using System;

namespace FridgeScan.Views;

public partial class ProductsPage : ContentPage
{
    private bool isSearchAnimating;

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

    private async void OnEditProductTapped(object sender, EventArgs e)
    {
        if (sender is Border border && border.BindingContext is Models.Product product)
        {
            var vm = (ProductsViewModel)BindingContext;
            if (vm.EditProductCommand.CanExecute(product))
                vm.EditProductCommand.Execute(product);
        }
    }

    // ── Toolbar pill event handlers ──────────────────────────────

    private async void OnSearchPillTapped(object sender, EventArgs e)
    {
        if (isSearchAnimating) return;

        if (BindingContext is not ProductsViewModel vm) return;

        isSearchAnimating = true;
        try
        {
            // Collapse any open filter/sort panels
            vm.IsFilterExpanded = false;
            vm.IsSortExpanded = false;

            // Show search expanded, hide collapsed pills
            SearchExpanded.IsVisible = true;
            SearchExpanded.Opacity = 0;

            // Fade out the collapsed toolbar pills
            await CollapsedToolbar.FadeTo(0, 150, Easing.CubicIn);

            // Show the search bar
            await SearchExpanded.FadeTo(1, 200, Easing.CubicOut);

            // Show secondary pills (Filter + Sort on second row)
            SecondaryPills.IsVisible = true;
            SecondaryPills.Opacity = 0;
            await SecondaryPills.FadeTo(1, 200, Easing.CubicOut);

            CollapsedToolbar.IsVisible = false;
            vm.IsSearchExpanded = true;
        }
        finally
        {
            isSearchAnimating = false;
        }

        // Focus search entry after animation settles
        SearchEntry.Focus();
    }

    private async void OnSearchDismissTapped(object sender, EventArgs e)
    {
        if (isSearchAnimating) return;

        if (BindingContext is not ProductsViewModel vm) return;

        isSearchAnimating = true;
        try
        {
            // Clear search text
            vm.SearchText = string.Empty;
            SearchEntry.Unfocus();

            // Hide secondary pills
            await SecondaryPills.FadeTo(0, 150, Easing.CubicIn);
            SecondaryPills.IsVisible = false;

            // Hide search bar
            await SearchExpanded.FadeTo(0, 150, Easing.CubicIn);
            SearchExpanded.IsVisible = false;

            // Show collapsed pills
            CollapsedToolbar.IsVisible = true;
            CollapsedToolbar.Opacity = 0;
            await CollapsedToolbar.FadeTo(1, 200, Easing.CubicOut);

            vm.IsSearchExpanded = false;
        }
        finally
        {
            isSearchAnimating = false;
        }
    }

    // ── Filter / Sort toggle handlers ────────────────────────────

    private void OnFilterPillTapped(object sender, EventArgs e)
    {
        ToggleFilterPanel();
    }

    private void OnSecondaryFilterTapped(object sender, EventArgs e)
    {
        ToggleFilterPanel();
    }

    private void ToggleFilterPanel()
    {
        if (BindingContext is ProductsViewModel vm)
        {
            // Close sort panel if open
            vm.IsSortExpanded = false;
            SortPanel.IsVisible = false;

            // Toggle filter panel
            vm.IsFilterExpanded = !vm.IsFilterExpanded;
            FilterPanel.IsVisible = vm.IsFilterExpanded;
            FilterPill.BackgroundColor = vm.IsFilterExpanded
                ? Color.FromArgb("#2A2E58")
                : Color.FromArgb("#1E1E3A");
        }
    }

    private void OnSortPillTapped(object sender, EventArgs e)
    {
        ToggleSortPanel();
    }

    private void OnSecondarySortTapped(object sender, EventArgs e)
    {
        ToggleSortPanel();
    }

    private void ToggleSortPanel()
    {
        if (BindingContext is ProductsViewModel vm)
        {
            // Close filter panel if open
            vm.IsFilterExpanded = false;
            FilterPanel.IsVisible = false;

            // Toggle sort panel
            vm.IsSortExpanded = !vm.IsSortExpanded;
            SortPanel.IsVisible = vm.IsSortExpanded;
            SortPill.BackgroundColor = vm.IsSortExpanded
                ? Color.FromArgb("#2A2E58")
                : Color.FromArgb("#1E1E3A");
        }
    }

    // ── Filter segment selection handlers ────────────────────────

    private void OnFilterSegmentExpiringTapped(object sender, EventArgs e)
    {
        if (BindingContext is ProductsViewModel vm)
        {
            vm.ActiveFilter = ProductFilterMode.ExpiringSoon;
            UpdateFilterPillAppearance(vm);
            ClosePanels();
        }
    }

    private void OnFilterSegmentExpiredTapped(object sender, EventArgs e)
    {
        if (BindingContext is ProductsViewModel vm)
        {
            vm.ActiveFilter = ProductFilterMode.Expired;
            UpdateFilterPillAppearance(vm);
            ClosePanels();
        }
    }

    private void OnFilterSegmentAllTapped(object sender, EventArgs e)
    {
        if (BindingContext is ProductsViewModel vm)
        {
            vm.ActiveFilter = ProductFilterMode.None;
            UpdateFilterPillAppearance(vm);
            ClosePanels();
        }
    }

    private void UpdateFilterPillAppearance(ProductsViewModel vm)
    {
        bool isActive = vm.ActiveFilter != ProductFilterMode.None;
        FilterDot.IsVisible = isActive;
        FilterLabel.Text = vm.ActiveFilter switch
        {
            ProductFilterMode.ExpiringSoon => "Expiring",
            ProductFilterMode.Expired => "Expired",
            _ => "Filter"
        };
        FilterIcon.TextColor = isActive
            ? Color.FromArgb("#D0BCFF")
            : Color.FromArgb("#CCCCDD");
        FilterLabel.TextColor = isActive
            ? Color.FromArgb("#D0BCFF")
            : Color.FromArgb("#CCCCDD");
        FilterPill.BackgroundColor = isActive
            ? Color.FromArgb("#2A2E58")
            : Color.FromArgb("#1E1E3A");
    }

    // ── Sort segment selection handlers ──────────────────────────

    private void OnSortSegmentAZTapped(object sender, EventArgs e)
    {
        if (BindingContext is ProductsViewModel vm)
        {
            vm.ActiveSort = ProductSortMode.Alphabetical;
            UpdateSortPillAppearance(vm);
            ClosePanels();
        }
    }

    private void OnSortSegmentExpiryTapped(object sender, EventArgs e)
    {
        if (BindingContext is ProductsViewModel vm)
        {
            vm.ActiveSort = ProductSortMode.ByExpiry;
            UpdateSortPillAppearance(vm);
            ClosePanels();
        }
    }

    private void UpdateSortPillAppearance(ProductsViewModel vm)
    {
        bool isActive = vm.ActiveSort != ProductSortMode.Alphabetical;
        SortDot.IsVisible = isActive;
        SortLabel.Text = isActive ? "Expiry" : "Sort";
        SortIcon.TextColor = isActive
            ? Color.FromArgb("#D0BCFF")
            : Color.FromArgb("#CCCCDD");
        SortLabel.TextColor = isActive
            ? Color.FromArgb("#D0BCFF")
            : Color.FromArgb("#CCCCDD");
        SortPill.BackgroundColor = isActive
            ? Color.FromArgb("#2A2E58")
            : Color.FromArgb("#1E1E3A");
    }

    // ── Panel helper ─────────────────────────────────────────────

    private void ClosePanels()
    {
        if (BindingContext is ProductsViewModel vm)
        {
            vm.IsFilterExpanded = false;
            vm.IsSortExpanded = false;
            FilterPanel.IsVisible = false;
            SortPanel.IsVisible = false;
        }
    }
}
