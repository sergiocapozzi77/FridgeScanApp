using System.Windows.Input;

namespace FridgeScan.Models;

public partial class Product : ObservableRecipient
{
    public ICommand DecreaseCommand { get; }
    public ICommand IncreaseCommand { get; }

    public ICommand RemoveCommand { get; }

    private Product()
    {

    }

    public Product(string name, string? category, int quantity)
    {
        this.name = name;
        this.category = category ?? "Other";
        this.quantity = quantity;
        DecreaseCommand = new Command(() =>
        {
            if (Quantity > 0)
            {
                Quantity--;
            }
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
    public string rowId;

    [ObservableProperty]
    public string name;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public int quantity;

    // New: product type/category for grouping (e.g., Dairy, Vegetables, Meat)
    [ObservableProperty]
    public string category;

    public override string ToString() => $"{Name} ({Quantity})";

}
