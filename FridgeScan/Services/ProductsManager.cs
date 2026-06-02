using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FridgeScan.Services
{
    public class ProductsManager
    {
        public ObservableCollection<Product> Products { get; set; }

        public void AddProduct(Product product)
        {
            Products.Add(product);
        }

        public void RemoveProduct(Product product)
        {
            Products.Remove(product);
        }

        internal void Init(List<Product> items)
        {
            Products = new ObservableCollection<Product>(items);
        }
    }
}
