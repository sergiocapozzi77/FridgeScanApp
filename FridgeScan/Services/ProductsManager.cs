namespace FridgeScan.Services;

public class ProductsManager
{
    public ObservableCollection<Product> Products { get; } = new();

    public void AddProduct(Product product) => Products.Add(product);

    public void RemoveProduct(Product product) => Products.Remove(product);

    internal void Init(List<Product> items)
    {
        // Remove items one at a time instead of Clear()
        // Clear() fires a Reset event which confuses SfListView's DataSource
        // and scrambles BindingContext assignments on visual items
        while (Products.Count > 0)
            Products.RemoveAt(Products.Count - 1);

        foreach (var item in items)
            Products.Add(item);
    }
}
