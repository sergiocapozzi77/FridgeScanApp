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
    private DateTime? expiryDate;

    [ObservableProperty]
    private bool isFrozen;

    public ProductDetailViewModel(ProductService productService, ProductsManager productsManager)
    {
        this.productService = productService;
        this.productsManager = productsManager;
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
                ExpiryDate = originalProduct.ExpiryDate;
                IsFrozen = originalProduct.IsFrozen;
            }
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (originalProduct == null) return;

        originalProduct.Name = ProductName;
        originalProduct.Quantity = Quantity;
        originalProduct.ExpiryDate = ExpiryDate;
        originalProduct.IsFrozen = IsFrozen;

        await productService.UpdateProductAsync(originalProduct);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private void ClearExpiry()
    {
        ExpiryDate = null;
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

        productsManager.RemoveProduct(originalProduct);
        await productService.DeleteProductAsync(originalProduct.RowId);
        await Shell.Current.GoToAsync("//products");
    }
}
