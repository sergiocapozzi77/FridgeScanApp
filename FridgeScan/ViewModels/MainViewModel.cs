using FridgeScan.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FridgeScan.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    public ObservableCollection<Product> Products { get; set; }

    public ObservableCollection<ListViewFoodCategory> GroupedProducts { get; } = new();

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
    private readonly ProductService productService;

    public string NewItemName
    {
        get => _newItemName;
        set
        {
            SetProperty(ref _newItemName, value);
            
        }
    }

    public ICommand AddItemCommand { get; }

    public MainViewModel(ProductService productService)
    {
        this.productService = productService;

        AddItemCommand = new Command(OnAddItem);
        LoadSuggestionsFromJson();

        _ = LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        var items = await productService.GetProductsAsync();

        Products = new ObservableCollection<Product>(items);
        Products.CollectionChanged += (s, e) => RefreshGrouping();
        foreach (var item in Products)
        {
            item.PropertyChanged += (s, e) => ProductPropertChanged(e, s as Product);
        }


        RefreshGrouping();
    }

    private async Task ProductPropertChanged(PropertyChangedEventArgs e, Product item)
    {
        if (e.PropertyName == nameof(Product.Quantity))
        {
            if (item.Quantity <= 0)
            {
                await RemoveProduct(item);
            } else
            {
                await productService.UpdateProductAsync(item);
            }
        }
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
        // Build lookup of current groups from Products
        var newGroups = Products
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Other" : p.Category)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 1. REMOVE groups that no longer exist
        for (int i = GroupedProducts.Count - 1; i >= 0; i--)
        {
            var existingGroup = GroupedProducts[i];

            if (!newGroups.ContainsKey(existingGroup.FoodCategory))
            {
                GroupedProducts.RemoveAt(i);
            }
        }

        // 2. UPDATE existing groups or ADD new ones
        foreach (var kvp in newGroups)
        {
            var category = kvp.Key;
            var items = kvp.Value;

            var existingGroup = GroupedProducts
                .FirstOrDefault(g => g.FoodCategory == category);

            if (existingGroup == null)
            {
                // Add new group
                GroupedProducts.Add(new ListViewFoodCategory(category, items));
            }
            else
            {
                // Update existing group items
                existingGroup.FoodMenuCollection.Clear();
                foreach (var item in items)
                    existingGroup.FoodMenuCollection.Add(item);
            }
        }

        // 3. Sort groups alphabetically
        var sorted = GroupedProducts.OrderBy(g => g.FoodCategory).ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            if (!ReferenceEquals(GroupedProducts[i], sorted[i]))
            {
                GroupedProducts.Move(GroupedProducts.IndexOf(sorted[i]), i);
            }
        }
    }

    public void OnAddItem()
    {
        AddItem(NewItemName);
        NewItemName = null;
    }

    async void AddItem(string item)
    {
        if (string.IsNullOrEmpty(item))
            return;

        var trimmed = item.Trim();

        var existing = Products.FirstOrDefault(x =>
                string.Equals(x.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.Quantity += 1;
            return;
        }

        var match = GrocerySuggestions
            .FirstOrDefault(x =>
                string.Equals(x.Name, trimmed, StringComparison.OrdinalIgnoreCase));

        var product = new Product
        {
            Name = trimmed,
            Quantity = 1,
            Category = match?.Category ?? "Other"
        };
        product.PropertyChanged += (s, e) => ProductPropertChanged(e, s as Product);

        Products.Add(product);
        await productService.AddOrUpdateQuantityAsync(product);
    }


    public async Task RemoveProduct(Product product)
    {
        if (product == null) return;

        product.PropertyChanged -= (s, e) => ProductPropertChanged(e, s as Product);
        Products.Remove(product);
        var success = await productService.DeleteProductAsync(product.RowId);
        if(success)
        {
        }
    }
}
