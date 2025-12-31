using System.Windows.Input;

namespace FridgeScan.Models;

public class Product : INotifyPropertyChanged
{
    public ICommand DecreaseCommand { get; }
    public ICommand IncreaseCommand { get; }

    public ICommand RemoveCommand { get; }

    private string _name = string.Empty;
    private int _quantity;
    private string _category = "Other";
    private string _rowId;

    public Product()
    {
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

    public string RowId
    {
        get => _rowId;
        set
        {
            if (_rowId != value)
            {
                _rowId = value;
                OnPropertyChanged();
            }
        }
    }

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
