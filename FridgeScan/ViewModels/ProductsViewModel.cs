using CommunityToolkit.Mvvm.Messaging;
using FridgeScan.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FridgeScan.ViewModels;

public partial class ProductsViewModel : BaseViewModel
{
  

    public ObservableCollection<Product> Products => productsManager.Products;

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
    private readonly ProductsManager productsManager;

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

    public ProductsViewModel(ProductService productService, ActivityService activityService, ProductsManager productsManager)
    {
        this.productService = productService;
        this.activityService = activityService;
        this.productsManager = productsManager;

        WeakReferenceMessenger.Default.Register<ProductMessage>(this, (r, m) =>
        {
            AddItem(m.Value.Name, m.Value.Category);
        });

        AddItemCommand = new Command(OnAddItem);
        BarcodeCommand = new Command(OnBarcodeCommand);
        LoadSuggestionsFromJson();
        
    }

    private void OnBarcodeCommand(object obj)
    {
        Application.Current.MainPage.Navigation.PushAsync(new BarcodeScannerPage());
    }

    [RelayCommand]
    private async Task EditProduct(Product product)
    {
        if (product == null) return;
        await Shell.Current.GoToAsync("ProductDetailPage", new Dictionary<string, object>
        {
            { "productId", product.RowId }
        });
    }

    public async Task LoadProductsAsync()
    {
        var items = await productService.GetProductsAsync();
        productsManager.Init(items);
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

    #region AddItem / RemoveItem

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

        var existing = productsManager.Products.FirstOrDefault(x =>
                string.Equals(x.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.Quantity += 1;
            await productService.UpdateProductAsync(existing);
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

        productsManager.AddProduct(product);

        await productService.AddOrUpdateQuantityAsync(product);
    }


    public async Task RemoveProduct(Product product)
    {
        if (product == null) return;

        productsManager.RemoveProduct(product);

        var success = await productService.DeleteProductAsync(product.RowId);

    }

    #endregion
}
