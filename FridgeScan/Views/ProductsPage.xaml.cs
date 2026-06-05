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

        // Remove Android Entry underline on the search field
        SearchEntry.HandlerChanged += OnSearchEntryHandlerChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ProductsViewModel vm)
        {
            vm.RefreshAfterEdit();
            UpdateFilterPillAppearance(vm);
            UpdateSortPillAppearance(vm);
            UpdateFilterSegmentHighlight();
            UpdateSortSegmentHighlight();

            // Hide panels on return, reset search
            vm.IsSearchExpanded = false;
            vm.IsFilterExpanded = false;
            vm.IsSortExpanded = false;
            CollapsedToolbar.IsVisible = true;
            ExpandedToolbar.IsVisible = false;
            FilterPanel.IsVisible = false;
            SortPanel.IsVisible = false;
        }
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

    // ── Search expand / collapse ─────────────────────────────────

    private void OnSearchPillTapped(object sender, EventArgs e)
    {
        if (BindingContext is not ProductsViewModel vm) return;

        // Collapse any open filter/sort panels
        vm.IsFilterExpanded = false;
        vm.IsSortExpanded = false;
        FilterPanel.IsVisible = false;
        SortPanel.IsVisible = false;

        // Switch to expanded toolbar
        CollapsedToolbar.IsVisible = false;
        ExpandedToolbar.IsVisible = true;
        vm.IsSearchExpanded = true;

        // Sync expanded pill labels with current state
        FilterLabelEx.Text = FilterLabel.Text;
        SortLabelEx.Text = SortLabel.Text;
        FilterPillExpanded.BackgroundColor = FilterPill.BackgroundColor;
        SortPillExpanded.BackgroundColor = SortPill.BackgroundColor;

        // Focus search entry
        SearchEntry.Focus();
    }

    private void OnSearchDismissTapped(object sender, EventArgs e)
    {
        if (BindingContext is not ProductsViewModel vm) return;

        vm.SearchText = string.Empty;
        SearchEntry.Unfocus();

        // Switch back to collapsed toolbar
        ExpandedToolbar.IsVisible = false;
        CollapsedToolbar.IsVisible = true;
        vm.IsSearchExpanded = false;
    }

    private void OnSearchEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (ExpandedToolbar.IsVisible)
            OnSearchDismissTapped(sender, e);
    }

    private void OnSearchEntryHandlerChanged(object sender, EventArgs e)
    {
#if ANDROID
        if (SearchEntry.Handler?.PlatformView is Android.Widget.EditText editText)
        {
            editText.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(
                Android.Graphics.Color.Transparent);
        }
#endif
    }

    // ── Filter / Sort toggle handlers ────────────────────────────

    private void OnFilterPillTapped(object sender, EventArgs e)
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
            var bg = vm.IsFilterExpanded
                ? Color.FromArgb("#2A2E58")
                : Color.FromArgb("#1E1E3A");
            FilterPill.BackgroundColor = bg;
            FilterPillExpanded.BackgroundColor = bg;
        }
    }

    private void OnSortPillTapped(object sender, EventArgs e)
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
            var bg = vm.IsSortExpanded
                ? Color.FromArgb("#2A2E58")
                : Color.FromArgb("#1E1E3A");
            SortPill.BackgroundColor = bg;
            SortPillExpanded.BackgroundColor = bg;
        }
    }

    // ── Filter segment selection handlers ────────────────────────

    private void OnFilterSegmentExpiringTapped(object sender, EventArgs e)
    {
        if (BindingContext is ProductsViewModel vm)
        {
            vm.ActiveFilter = ProductFilterMode.ExpiringSoon;
            UpdateFilterPillAppearance(vm);
            UpdateFilterSegmentHighlight();
            ClosePanels();
        }
    }

    private void OnFilterSegmentExpiredTapped(object sender, EventArgs e)
    {
        if (BindingContext is ProductsViewModel vm)
        {
            vm.ActiveFilter = ProductFilterMode.Expired;
            UpdateFilterPillAppearance(vm);
            UpdateFilterSegmentHighlight();
            ClosePanels();
        }
    }

    private void OnFilterSegmentAllTapped(object sender, EventArgs e)
    {
        if (BindingContext is ProductsViewModel vm)
        {
            vm.ActiveFilter = ProductFilterMode.None;
            UpdateFilterPillAppearance(vm);
            UpdateFilterSegmentHighlight();
            ClosePanels();
        }
    }

    private void UpdateFilterPillAppearance(ProductsViewModel vm)
    {
        bool isActive = vm.ActiveFilter != ProductFilterMode.None;
        var labelText = vm.ActiveFilter switch
        {
            ProductFilterMode.ExpiringSoon => "Expiring",
            ProductFilterMode.Expired => "Expired",
            _ => "Filter"
        };
        var iconColor = isActive
            ? Color.FromArgb("#D0BCFF") : Color.FromArgb("#CCCCDD");
        var textColor = isActive
            ? Color.FromArgb("#D0BCFF") : Color.FromArgb("#CCCCDD");
        var bgColor = isActive
            ? Color.FromArgb("#2A2E58") : Color.FromArgb("#1E1E3A");

        // Collapsed pills
        FilterDot.IsVisible = isActive;
        FilterLabel.Text = labelText;
        FilterIcon.TextColor = iconColor;
        FilterLabel.TextColor = textColor;
        FilterPill.BackgroundColor = bgColor;

        // Expanded toolbar copy
        FilterLabelEx.Text = labelText;
        FilterPillExpanded.BackgroundColor = bgColor;
    }

    // ── Sort segment selection handlers ──────────────────────────

    private void OnSortSegmentAZTapped(object sender, EventArgs e)
    {
        if (BindingContext is ProductsViewModel vm)
        {
            vm.ActiveSort = ProductSortMode.Alphabetical;
            UpdateSortPillAppearance(vm);
            UpdateSortSegmentHighlight();
            ClosePanels();
        }
    }

    private void OnSortSegmentExpiryTapped(object sender, EventArgs e)
    {
        if (BindingContext is ProductsViewModel vm)
        {
            vm.ActiveSort = ProductSortMode.ByExpiry;
            UpdateSortPillAppearance(vm);
            UpdateSortSegmentHighlight();
            ClosePanels();
        }
    }

    private void UpdateSortPillAppearance(ProductsViewModel vm)
    {
        bool isActive = vm.ActiveSort != ProductSortMode.Alphabetical;
        var labelText = isActive ? "Expiry" : "Sort";
        var iconColor = isActive
            ? Color.FromArgb("#D0BCFF") : Color.FromArgb("#CCCCDD");
        var textColor = isActive
            ? Color.FromArgb("#D0BCFF") : Color.FromArgb("#CCCCDD");
        var bgColor = isActive
            ? Color.FromArgb("#2A2E58") : Color.FromArgb("#1E1E3A");

        // Collapsed pills
        SortDot.IsVisible = isActive;
        SortLabel.Text = labelText;
        SortIcon.TextColor = iconColor;
        SortLabel.TextColor = textColor;
        SortPill.BackgroundColor = bgColor;

        // Expanded toolbar copy
        SortLabelEx.Text = labelText;
        SortPillExpanded.BackgroundColor = bgColor;
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

    // ── Segment highlight helpers ──────────────────────────────────

    private void UpdateFilterSegmentHighlight()
    {
        if (BindingContext is not ProductsViewModel vm) return;

        bool isExpiring = vm.ActiveFilter == ProductFilterMode.ExpiringSoon;
        bool isExpired = vm.ActiveFilter == ProductFilterMode.Expired;
        bool isAll = vm.ActiveFilter == ProductFilterMode.None;

        FilterSegmentExpiring.BackgroundColor = isExpiring
            ? Color.FromArgb("#2A2E58") : Colors.Transparent;
        FilterLabelExpiring.TextColor = isExpiring ? Colors.White : Color.FromArgb("#8888AA");

        FilterSegmentExpired.BackgroundColor = isExpired
            ? Color.FromArgb("#2A2E58") : Colors.Transparent;
        FilterLabelExpired.TextColor = isExpired ? Colors.White : Color.FromArgb("#8888AA");

        FilterSegmentAll.BackgroundColor = isAll
            ? Color.FromArgb("#2A2E58") : Colors.Transparent;
        FilterLabelAll.TextColor = isAll ? Colors.White : Color.FromArgb("#8888AA");
    }

    private void UpdateSortSegmentHighlight()
    {
        if (BindingContext is not ProductsViewModel vm) return;

        bool isAZ = vm.ActiveSort == ProductSortMode.Alphabetical;
        bool isExpiry = vm.ActiveSort == ProductSortMode.ByExpiry;

        SortSegmentAZ.BackgroundColor = isAZ
            ? Color.FromArgb("#2A2E58") : Colors.Transparent;
        SortLabelAZ.TextColor = isAZ ? Colors.White : Color.FromArgb("#8888AA");

        SortSegmentExpiry.BackgroundColor = isExpiry
            ? Color.FromArgb("#2A2E58") : Colors.Transparent;
        SortLabelExpiry.TextColor = isExpiry ? Colors.White : Color.FromArgb("#8888AA");
    }
}
