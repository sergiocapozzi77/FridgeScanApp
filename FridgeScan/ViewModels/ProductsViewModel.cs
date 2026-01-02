using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FridgeScan.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FridgeScan.ViewModels;

public partial class ProductsViewModel : BaseViewModel
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
    private readonly ActivityService activityService;

    public string NewItemName
    {
        get => _newItemName;
        set
        {
            SetProperty(ref _newItemName, value);
            
        }
    }

    public ICommand AddItemCommand { get; }

    public ICommand BarcodeCommand { get; }

    public ProductsViewModel(ProductService productService, ActivityService activityService)
    {
        this.productService = productService;
        this.activityService = activityService;

        WeakReferenceMessenger.Default.Register<PropertyChangedMessage<int>>(this, OnQuantityChanged);
        WeakReferenceMessenger.Default.Register<ProductMessage>(this, (r, m) =>
        {
            AddItem(m.Value.Name, m.Value.Category);
        });

        AddItemCommand = new Command(OnAddItem);
        BarcodeCommand = new Command(OnBarcodeCommand);
        LoadSuggestionsFromJson();

        _ = LoadProductsAsync();
        
    }

    private void OnBarcodeCommand(object obj)
    {
        Application.Current.MainPage.Navigation.PushAsync(new BarcodeScannerPage());
    }

    private void OnQuantityChanged(object recipient, PropertyChangedMessage<int> message)
    {
        if (message.PropertyName == nameof(Product.Quantity))
        {
            // message.Sender is the Product instance
            var product = (Product)message.Sender;

            int oldValue = message.OldValue;
            int newValue = message.NewValue;

            // React however you want
            HandleQuantityChanged(product, oldValue, newValue);
        }
    }

    private async void HandleQuantityChanged(Product product, int oldValue, int newValue)
    {
        if(oldValue != newValue)
        {
            if (product.Quantity <= 0)
            {
                await RemoveProduct(product);
            }
            else
            {
                await productService.UpdateProductAsync(product);
            }
        }
    }

    public async Task LoadProductsAsync()
    {
        var items = await productService.GetProductsAsync();

        Products = new ObservableCollection<Product>(items);
      //  Products.CollectionChanged += (s, e) => RefreshGrouping();

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
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Other" : p.Category)
            .OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            GroupedProducts.Add(
                new ListViewFoodCategory(group.Key, group.ToList())
            );
        }
    }

    private void AddProductToGroups(Product product)
    {
        var category = string.IsNullOrWhiteSpace(product.Category)
            ? "Other"
            : product.Category;

        var group = GroupedProducts.FirstOrDefault(g => g.FoodCategory == category);

        // Create the group if missing
        if (group == null)
        {
            group = new ListViewFoodCategory(category, new List<Product>());
            GroupedProducts.Add(group);

            // keep ordering alphabetical
            var ordered = GroupedProducts.OrderBy(g => g.FoodCategory).ToList();
            GroupedProducts.Clear();
            foreach (var g in ordered)
                GroupedProducts.Add(g);
        }

        group.FoodMenuCollection.Add(product);
    }

    private void RemoveProductFromGroups(Product product)
    {
        var category = string.IsNullOrWhiteSpace(product.Category)
            ? "Other"
            : product.Category;

        var group = GroupedProducts.FirstOrDefault(g => g.FoodCategory == category);
        if (group == null)
            return;

        group.FoodMenuCollection.Remove(product);

        // Remove empty groups to keep UI tidy
        if (group.FoodMenuCollection.Count == 0)
            GroupedProducts.Remove(group);
    }


    public void OnAddItem()
    {
        AddItem(NewItemName);
        NewItemName = null;
    }

    async void AddItem(string name, string? category = null)
    {
        if (string.IsNullOrEmpty(name))
            return;

        var trimmed = name.Trim();

        var existing = Products.FirstOrDefault(x =>
                string.Equals(x.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.Quantity += 1;
            return;
        }

        if (string.IsNullOrEmpty(category))
        {
            var match = GrocerySuggestions
                .FirstOrDefault(x =>
                    string.Equals(x.Name, trimmed, StringComparison.OrdinalIgnoreCase));
            category = match?.Category;
        }

        var product = new Product(trimmed, category, 1);

        Products.Add(product);
        AddProductToGroups(product);

         await productService.AddOrUpdateQuantityAsync(product);
    }


    public async Task RemoveProduct(Product product)
    {
        if (product == null) return;

        Products.Remove(product);
        RemoveProductFromGroups(product);

        var success = await productService.DeleteProductAsync(product.RowId);

    }
}
