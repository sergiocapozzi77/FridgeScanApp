using CommunityToolkit.Mvvm.Messaging;
using FridgeScan.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FridgeScan.ViewModels;

public enum ProductFilterMode { None, ExpiringSoon, Expired }
public enum ProductSortMode { Alphabetical, ByExpiry }

public partial class ProductsViewModel : BaseViewModel
{
  

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

    // -- New filter/sort/search state --

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isSearchExpanded;

    [ObservableProperty]
    private bool isFilterExpanded;

    [ObservableProperty]
    private bool isSortExpanded;

    [ObservableProperty]
    private ProductFilterMode activeFilter;

    [ObservableProperty]
    private ProductSortMode activeSort;

    // -- End new state --

    public ProductsViewModel(ProductService productService, ActivityService activityService, ProductsManager productsManager)
    {
        this.productService = productService;
        this.activityService = activityService;
        this.productsManager = productsManager;

        WeakReferenceMessenger.Default.Register<ProductMessage>(this, (r, m) =>
        {
            AddItem(m.Value.Name, m.Value.Category, m.Value.ExpiryDate);
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

        RefreshDisplay();
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

        if (productsManager.Products == null)
            return;

        var groups = productsManager.Products
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Other" : p.Category)
            .OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            GroupedProducts.Add(
                new ListViewFoodCategory(group.Key, group.ToList())
            );
        }
    }

    public void RefreshDisplay()
    {
        GroupedProducts.Clear();

        if (productsManager.Products == null)
            return;

        IEnumerable<Product> query = productsManager.Products;

        // Apply search filter
        if (!string.IsNullOrEmpty(SearchText))
        {
            query = query.Where(p =>
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        // Apply expiry filter
        if (ActiveFilter == ProductFilterMode.ExpiringSoon)
        {
            // Shows products expiring within 7 days OR already expired
            query = query.Where(p =>
                p.DaysUntilExpiry.HasValue && p.DaysUntilExpiry.Value <= 7);
        }
        else if (ActiveFilter == ProductFilterMode.Expired)
        {
            query = query.Where(p =>
                p.DaysUntilExpiry.HasValue && p.DaysUntilExpiry.Value < 0);
        }

        // Group by category
        var groups = query
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Other" : p.Category);

        // Apply sort mode
        if (ActiveSort == ProductSortMode.Alphabetical)
        {
            var sorted = groups
                .OrderBy(g => g.Key)
                .Select(g => new ListViewFoodCategory(
                    g.Key,
                    g.OrderBy(p => p.Name).ToList()))
                .ToList();

            foreach (var g in sorted)
                GroupedProducts.Add(g);
        }
        else // ByExpiry
        {
            var sorted = groups
                .OrderBy(g => g.Min(p => p.DaysUntilExpiry ?? int.MaxValue))
                .Select(g => new ListViewFoodCategory(
                    g.Key,
                    g.OrderBy(p => p.DaysUntilExpiry ?? int.MaxValue)
                     .ThenBy(p => p.Name)
                     .ToList()))
                .ToList();

            foreach (var g in sorted)
                GroupedProducts.Add(g);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshDisplay();
    }

    partial void OnActiveFilterChanged(ProductFilterMode value)
    {
        RefreshDisplay();
    }

    partial void OnActiveSortChanged(ProductSortMode value)
    {
        RefreshDisplay();
    }

    /// <summary>
    /// Syncs groupings after edits/deletes from detail page.
    /// Now respects active filter/sort/search by calling RefreshDisplay.
    /// </summary>
    public void RefreshAfterEdit()
    {
        RefreshDisplay();
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

    async void AddItem(string name, string? category = null, DateTime? expiryDate = null)
    {
        if (string.IsNullOrEmpty(name))
            return;

        var trimmed = name.Trim();

        var existing = productsManager.Products.FirstOrDefault(x =>
                string.Equals(x.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.Quantity += 1;
            if (expiryDate.HasValue)
                existing.ExpiryDate = expiryDate;
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
        if (expiryDate.HasValue)
            product.ExpiryDate = expiryDate;

        productsManager.AddProduct(product);
        RefreshDisplay();

         await productService.AddOrUpdateQuantityAsync(product);
    }


    public async Task RemoveProduct(Product product)
    {
        if (product == null) return;

        productsManager.RemoveProduct(product);
        RemoveProductFromGroups(product);

        var success = await productService.DeleteProductAsync(product.RowId);

    }
}
