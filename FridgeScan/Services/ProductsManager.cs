namespace FridgeScan.Services;

public class ProductsManager
{
    public ObservableCollection<Product> Products { get; } = new();

    public void AddProduct(Product product) => Products.Add(product);

    public void RemoveProduct(Product product) => Products.Remove(product);

    internal void Init(List<Product> items)
    {
        Products.Clear();
        foreach (var item in items)
            Products.Add(item);
    }
}
