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

    /// <summary>
    /// Syncs groupings in-place after edits/deletes from detail page.
    /// Preserves scroll position by avoiding GroupedProducts.Clear().
    /// </summary>
    public void RefreshAfterEdit()
    {
        if (productsManager.Products == null) return;

        var desired = productsManager.Products
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Other" : p.Category)
            .OrderBy(g => g.Key)
            .ToList();

        var desiredKeys = desired.Select(g => g.Key).ToHashSet();

        // Remove groups that no longer have any products
        for (int i = GroupedProducts.Count - 1; i >= 0; i--)
        {
            if (!desiredKeys.Contains(GroupedProducts[i].FoodCategory))
                GroupedProducts.RemoveAt(i);
        }

        var existingGroups = GroupedProducts.ToDictionary(g => g.FoodCategory);
        int insertIndex = 0;

        foreach (var group in desired)
        {
            if (existingGroups.TryGetValue(group.Key, out var existingGroup))
            {
                // Sync products in this group
                var desiredProducts = group.ToList();
                var desiredIds = desiredProducts.Select(p => p.RowId).ToHashSet();

                // Remove products deleted or moved to another category
                for (int i = existingGroup.FoodMenuCollection.Count - 1; i >= 0; i--)
                {
                    if (!desiredIds.Contains(existingGroup.FoodMenuCollection[i].RowId))
                        existingGroup.FoodMenuCollection.RemoveAt(i);
                }

                // Add products that moved into this category
                foreach (var product in desiredProducts)
                {
                    if (!existingGroup.FoodMenuCollection.Any(p => p.RowId == product.RowId))
                        existingGroup.FoodMenuCollection.Add(product);
                }

                // Ensure alphabetical ordering of groups
                var currentIndex = GroupedProducts.IndexOf(existingGroup);
                if (currentIndex != insertIndex && currentIndex >= 0)
                    GroupedProducts.Move(currentIndex, insertIndex);
            }
            else
            {
                // New group
                GroupedProducts.Insert(insertIndex, new ListViewFoodCategory(group.Key, group.ToList()));
            }
            insertIndex++;
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
        AddProductToGroups(product);

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
