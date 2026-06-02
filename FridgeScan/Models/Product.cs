using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FridgeScan.Models;

public partial class Product : ObservableRecipient
{
    public Product(string name, string? category, int quantity)
    {
        this.name = name;
        this.category = category ?? "Other";
        this.quantity = quantity;
    }

    [ObservableProperty]
    private string rowId;

    [ObservableProperty]
    public string name;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private int quantity;

    [ObservableProperty]
    private string category;

    [ObservableProperty]
    public bool isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DaysUntilExpiry))]
    [NotifyPropertyChangedFor(nameof(ShowExpiryBadge))]
    [NotifyPropertyChangedFor(nameof(ExpiryDisplayText))]
    [NotifyPropertyChangedFor(nameof(ExpiryColor))]
    private DateTime? expiryDate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowFrozenIcon))]
    private bool isFrozen;

    // Computed properties

    public int? DaysUntilExpiry =>
        ExpiryDate.HasValue
            ? (int?)(ExpiryDate.Value.Date - DateTime.Today.Date).TotalDays
            : null;

    public bool ShowExpiryBadge =>
        DaysUntilExpiry.HasValue && DaysUntilExpiry.Value <= 3;

    public string ExpiryDisplayText => DaysUntilExpiry switch
    {
        < 0 => "Expired",
        0   => "Today",
        <= 3 => $"{DaysUntilExpiry}d left",
        _   => null
    };

    public Color ExpiryColor => DaysUntilExpiry switch
    {
        < 0 => Color.FromArgb("#2E1E1E"),   // Error surface (tonal red)
        0   => Color.FromArgb("#3A2E28"),   // Warning surface (tonal amber)
        _   => Color.FromArgb("#2A2E58"),   // Surface container high (tonal neutral)
    };

    public bool ShowFrozenIcon => isFrozen;

    [RelayCommand]
    private void ToggleSelect()
    {
        IsSelected = !IsSelected;
    }

    public override string ToString() => $"{Name} ({Quantity})";
}
