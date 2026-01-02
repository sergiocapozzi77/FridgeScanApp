using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FridgeScan.Services
{
    using System.Net.Http.Json;
    using System.Text.Json;

    public class OpenFoodFactsService
    {
        private readonly HttpClient _http;

        public OpenFoodFactsService(HttpClient httpClient = null)
        {
            _http = httpClient ?? new HttpClient();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "FridgaScan/1.0 (MAUI .NET 9)"
            );
        }

        public async Task<ProductInfo> GetProductAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                throw new ArgumentException("Barcode cannot be empty.", nameof(barcode));

            var url = $"https://world.openfoodfacts.org/api/v2/product/{barcode}.json";

            using var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;

            if (!root.TryGetProperty("product", out var product))
                return null;

            string name = product.TryGetProperty("product_name", out var nameProp)
                ? nameProp.GetString()
                : null;

            string category = product.TryGetProperty("categories", out var catProp)
                ? catProp.GetString()?.Split(',').FirstOrDefault()?.Trim()
                : null;

            string imageUrl = product.TryGetProperty("image_url", out var imgProp)
                ? imgProp.GetString()
                : null;

            string thumbUrl = product.TryGetProperty("image_thumb_url", out var thumb)
             ? thumb.GetString()
             : null;
            

            return new ProductInfo
            {
                Barcode = barcode,
                Name = name,
                Category = category,
                ImageUrl = imageUrl,
                ThumbUrl = thumbUrl
            };
        }
    }
}
