using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace FridgeScan.Models;

public partial class Product : ObservableRecipient
{
    public ICommand DecreaseCommand { get; }
    public ICommand IncreaseCommand { get; }
    public ICommand RemoveCommand { get; }

    public Product(string name, string? category, int quantity)
    {
        this.name = name;
        this.category = category ?? "Other";
        this.quantity = quantity;

        DecreaseCommand = new Command(() =>
        {
            if (Quantity > 0)
                Quantity--;
        });

        IncreaseCommand = new Command(() =>
        {
            Quantity++;
        });

        RemoveCommand = new Command(() =>
        {
            Quantity = 0;
        });
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

    // FIXED: no parameter needed
    [RelayCommand]
    private void ToggleSelect()
    {
        IsSelected = !IsSelected;
    }

    public override string ToString() => $"{Name} ({Quantity})";
}
