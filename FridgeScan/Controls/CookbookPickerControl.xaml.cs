using System.Collections.ObjectModel;
using FridgeScan.Models;
using FridgeScan.Services;

namespace FridgeScan.Controls;

public partial class CookbookPickerControl : ContentView
{
    private readonly CookbookService? _cookbookService;

    public static readonly BindableProperty IsVisibleProperty =
        BindableProperty.Create(nameof(IsVisible), typeof(bool), typeof(CookbookPickerControl), false,
            propertyChanged: OnIsVisibleChanged);

    public static readonly BindableProperty PreSelectedCookbookIdsProperty =
        BindableProperty.Create(nameof(PreSelectedCookbookIds), typeof(IList<string>), typeof(CookbookPickerControl), null);

    public bool IsVisible
    {
        get => (bool)GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    public IList<string> PreSelectedCookbookIds
    {
        get => (IList<string>)GetValue(PreSelectedCookbookIdsProperty);
        set => SetValue(PreSelectedCookbookIdsProperty, value);
    }

    public ObservableCollection<Cookbook> AllCookbooks { get; } = new();

    /// <summary>Fired with the list of selected cookbook IDs when the user taps Save.</summary>
    public event EventHandler<IList<string>>? Saved;

    /// <summary>Fired when the user cancels or taps the backdrop.</summary>
    public event EventHandler? Cancelled;

    public CookbookPickerControl()
    {
        InitializeComponent();
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _cookbookService = services?.GetService<CookbookService>();
    }

    private static async void OnIsVisibleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CookbookPickerControl)bindable;
        if ((bool)newValue)
            await control.LoadCookbooksAsync();
    }

    private async Task LoadCookbooksAsync()
    {
        if (_cookbookService == null) return;
        try
        {
            var cookbooks = await _cookbookService.GetCookbooksAsync();
            var preselected = PreSelectedCookbookIds;

            AllCookbooks.Clear();
            foreach (var c in cookbooks)
            {
                c.IsSelected = preselected?.Contains(c.RowId) == true;
                AllCookbooks.Add(c);
            }
        }
        catch
        {
            // Silently handle — the panel remains open with whatever was loaded
        }
    }

    private void OnCookbookTapped(object? sender, TappedEventArgs e)
    {
        if (sender is View view && view.BindingContext is Cookbook cookbook)
            cookbook.IsSelected = !cookbook.IsSelected;
    }

    private async void OnCreateNewCookbook(object? sender, TappedEventArgs e)
    {
        if (_cookbookService == null) return;

        var name = await Shell.Current.DisplayPromptAsync("New Cookbook",
            "Enter a name:", "Create", "Cancel");

        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            var created = await _cookbookService.CreateCookbookAsync(name.Trim());
            if (created != null)
            {
                created.IsSelected = true;
                AllCookbooks.Add(created);
            }
        }
        catch
        {
            // Silently handle
        }
    }

    private void OnSaveClicked(object? sender, TappedEventArgs e)
    {
        var selectedIds = AllCookbooks
            .Where(c => c.IsSelected)
            .Select(c => c.RowId)
            .ToList();

        IsVisible = false;
        Saved?.Invoke(this, selectedIds);
    }

    private void OnCancelClicked(object? sender, TappedEventArgs e)
    {
        IsVisible = false;
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    private void OnBackdropTapped(object? sender, TappedEventArgs e)
    {
        OnCancelClicked(sender, e);
    }
}
