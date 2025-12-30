using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FridgeScan.Models;

public class Product : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private int _quantity;
    private string _category = "Other";

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = value;
                OnPropertyChanged();
            }
        }
    }

    // New: product type/category for grouping (e.g., Dairy, Vegetables, Meat)
    public string Category
    {
        get => _category;
        set
        {
            if (_category != value)
            {
                _category = value;
                OnPropertyChanged();
            }
        }
    }

    public override string ToString() => $"{Name} ({Quantity})";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
