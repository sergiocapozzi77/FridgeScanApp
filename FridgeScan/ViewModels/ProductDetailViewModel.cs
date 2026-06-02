using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FridgeScan.Models;
using FridgeScan.Services;

namespace FridgeScan.ViewModels;

public partial class ProductDetailViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ProductService productService;
    private readonly ProductsManager productsManager;
    private Product originalProduct;

    [ObservableProperty]
    private string productName;

    [ObservableProperty]
    private int quantity;

    [ObservableProperty]
    private string category;

    [ObservableProperty]
    private DateTime expiryDateValue = DateTime.Today;

    [ObservableProperty]
    private bool hasExpiryDate;

    [ObservableProperty]
    private bool isFrozen;

    public List<string> AvailableCategories { get; } = new()
    {
        "Baby", "Bakery", "Beverages", "Breakfast & Cereal", "Condiments & Dressing",
        "Cooking & Baking", "Dairy", "Deli", "Frozen Foods", "Grains",
        "Pasta & Sides", "Health & Personal Care", "Household & Cleaning", "Meat",
        "Pet Supplies", "Produce", "Seafood", "Snacks", "Soups & Canned Food",
        "Wine, Beer & Spirit", "Other"
    };

    public ProductDetailViewModel(ProductService productService, ProductsManager productsManager)
    {
        this.productService = productService;
        this.productsManager = productsManager;
    }

    partial void OnHasExpiryDateChanged(bool value)
    {
        if (!value)
            ExpiryDateValue = DateTime.Today;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("productId", out var id))
        {
            var productId = id?.ToString();
            if (string.IsNullOrEmpty(productId)) return;

            originalProduct = productsManager.Products.FirstOrDefault(p => p.RowId == productId);
            if (originalProduct != null)
            {
                ProductName = originalProduct.Name;
                Quantity = originalProduct.Quantity;
                Category = originalProduct.Category;
                HasExpiryDate = originalProduct.ExpiryDate.HasValue;
                if (originalProduct.ExpiryDate.HasValue)
                    ExpiryDateValue = originalProduct.ExpiryDate.Value;
                IsFrozen = originalProduct.IsFrozen;
            }
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (originalProduct == null) return;

        try
        {
            originalProduct.Name = ProductName;
            originalProduct.Quantity = Quantity;
            originalProduct.Category = Category;
            originalProduct.ExpiryDate = HasExpiryDate ? ExpiryDateValue : null;
            originalProduct.IsFrozen = IsFrozen;

            await productService.UpdateProductAsync(originalProduct);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to save: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private void ClearExpiry()
    {
        HasExpiryDate = false;
    }

    [RelayCommand]
    private void DecreaseQuantity()
    {
        if (Quantity > 0)
            Quantity--;
    }

    [RelayCommand]
    private void IncreaseQuantity()
    {
        Quantity++;
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (originalProduct == null) return;

        bool confirmed = await Shell.Current.DisplayAlert(
            "Delete Product",
            $"Are you sure you want to delete \"{originalProduct.Name}\"?",
            "Delete", "Cancel");

        if (!confirmed) return;

        try
        {
            var success = await productService.DeleteProductAsync(originalProduct.RowId);
            if (success)
            {
                productsManager.RemoveProduct(originalProduct);
                await Shell.Current.GoToAsync("//ProductsPage");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to delete product.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to delete: {ex.Message}", "OK");
        }
    }
}
