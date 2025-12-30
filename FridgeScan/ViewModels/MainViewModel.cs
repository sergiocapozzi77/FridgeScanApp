using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FridgeScan.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    public ObservableCollection<Product> Products { get; }

    public ObservableCollection<ListViewFoodCategory> GroupedProducts { get; } = new();

    public ICommand IncreaseCommand { get; }
    public ICommand DecreaseCommand { get; }
    public ICommand RemoveCommand { get; }

    public ObservableCollection<GroceryItem> GrocerySuggestions { get; } = new();
    public ObservableCollection<string> RecentItems { get; } = new();

    private GroceryItem _selectedGrocerySuggestion;
    public GroceryItem SelectedGrocerySuggestion
    {
        get => _selectedGrocerySuggestion;
        set
        {
            SetProperty(ref _selectedGrocerySuggestion, value);
            if (value != null)
            {

                AddItem(SelectedGrocerySuggestion.Name);
                SelectedGrocerySuggestion = null;
            }
        }
    }

    private string _newItemName;
    public string NewItemName
    {
        get => _newItemName;
        set
        {
            SetProperty(ref _newItemName, value);
            
        }
    }

    public ICommand AddItemCommand { get; }

    public MainViewModel()
    {

        IncreaseCommand = new Command<Product>(p => IncreaseQuantity(p));
        DecreaseCommand = new Command<Product>(p =>
        {
            DecreaseQuantity(p);
            if (p.Quantity <= 0)
                RemoveProduct(p);
        });
        RemoveCommand = new Command<Product>(RemoveProduct);

        AddItemCommand = new Command(OnAddItem);
        LoadSuggestionsFromJson();


        var defaults = new List<Product>
            {
                new Product { Name = "Milk", Quantity = 2, Type = "Dairy" },
                new Product { Name = "Eggs", Quantity = 12, Type = "Dairy" },
                new Product { Name = "Yogurt", Quantity = 3, Type = "Dairy" },
                new Product { Name = "Lettuce", Quantity = 1, Type = "Vegetables" },
                new Product { Name = "Tomatoes", Quantity = 4, Type = "Vegetables" },
                new Product { Name = "Chicken Breast", Quantity = 2, Type = "Meat" },
                new Product { Name = "Apples", Quantity = 6, Type = "Fruit" },
                new Product { Name = "Bread", Quantity = 1, Type = "Bakery" },
                new Product { Name = "Pork", Quantity = 1, Type = "Dairy" },
            };

        Products = new ObservableCollection<Product>(defaults);
        Products.CollectionChanged += (s, e) => RefreshGrouping();
        RefreshGrouping();
    }

    private async void LoadSuggestionsFromJson()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("grocery.json");
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var items = System.Text.Json.JsonSerializer.Deserialize<List<GroceryItem>>(json, options);

        GrocerySuggestions.Clear();
        foreach (var item in items)
            GrocerySuggestions.Add(item);
    }


    public void RefreshGrouping()
    {
        GroupedProducts.Clear();

        var groups = Products
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Type) ? "Other" : p.Type)
            .OrderBy(g => g.Key);

        foreach (var g in groups)
            GroupedProducts.Add(new ListViewFoodCategory(g.Key, g.ToList()));
    }

    public void OnAddItem()
    {
        AddItem(NewItemName);
        NewItemName = null;
    }

    void AddItem(string item)
    {
        if (string.IsNullOrEmpty(item))
            return;

        var trimmed = item.Trim();

        // Try find in suggestion list to get correct Type
        var match = GrocerySuggestions
            .FirstOrDefault(x =>
                string.Equals(x.Name, trimmed, StringComparison.OrdinalIgnoreCase));

        string type = match?.Category ?? "Other";

        Products.Add(new Product
        {
            Name = trimmed,
            Quantity = 1,
            Type = type
        });

        // Maintain recent list (no duplicates, last 5)
        if (!RecentItems.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            RecentItems.Insert(0, trimmed);

        while (RecentItems.Count > 5)
            RecentItems.RemoveAt(RecentItems.Count - 1);
    }

    public void IncreaseQuantity(Product product, int delta = 1)
    {
        if (product == null) return;
        product.Quantity += delta;
    }

    public void DecreaseQuantity(Product product, int delta = 1)
    {
        if (product == null) return;
        product.Quantity = Math.Max(0, product.Quantity - delta);
    }

    public void RemoveProduct(Product product)
    {
        if (product == null) return;
        Products.Remove(product);
        RefreshGrouping();
    }
}
