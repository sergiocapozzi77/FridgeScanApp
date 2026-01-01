using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FridgeScan.Services
{
    public class ProductService
    {
        private readonly HttpClient _http;
        private readonly ActivityService activityService;
        private const string Endpoint = "https://fra.cloud.appwrite.io/v1";
        private const string ProjectId = "6954045e003c75c1c3bf";
        private const string DatabaseId = "695404ac0021bf7d9707";
        private const string CollectionId = "products";

        public ProductService(ActivityService activityService)
        {
            var apiKey = Secrets.AppWriteApiKey;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("X-Appwrite-Project", ProjectId);
            _http.DefaultRequestHeaders.Add("X-Appwrite-Key", apiKey);
            this.activityService = activityService;
        }

        public async Task<List<Product>> GetProductsAsync(string[]? queries = null)
        {
            try
            {
                var baseUrl = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{CollectionId}/rows";

                string queryString = string.Empty;

                if (queries is { Length: > 0 })
                {
                    var encoded = queries
                        .Select((q, index) =>
                            $"queries[{index}]={Uri.EscapeDataString(q)}");

                    queryString = "?" + string.Join("&", encoded);
                }

                var url = baseUrl + queryString;

                var response = await _http.GetFromJsonAsync<AppwriteRowsResponse>(url);

                return response?.Rows?.Select((Func<AppwriteRow, Product>)(r => new Product(r.Name, r.Category, r.Quantity)
                {
                    RowId = r.Id
                })).ToList() ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching products: {ex.Message}");
                return new List<Product>();
            }
        }


        public async Task<AppwriteRow?> AddOrUpdateQuantityAsync(Product product)
        {
            var existing = await GetProductsAsync(new[]
{
    $@"{{""method"":""equal"",""attribute"":""name"",""values"":[""{product.Name}""]}}"
});


            if (existing.Count > 0)
            {
                var existingProduct = existing[0];
                existingProduct.Quantity += product.Quantity;
                return await UpdateProductAsync(existingProduct);
            }
            else
            {
                return await AddProductAsync(product);
            }
        }

        public async Task<AppwriteRow> AddProductAsync(Product product)
        {
            try
            {
                var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{CollectionId}/rows";

                var body = new
                {
                    rowId = GenerateId(),
                    data = new
                    {
                        name = product.Name,
                        quantity = product.Quantity,
                        category = product.Category
                    }
                };

                var response = await _http.PostAsJsonAsync(url, body);

                response.EnsureSuccessStatusCode();

                // Deserialize the created row
                var created = await response.Content.ReadFromJsonAsync<AppwriteRow>();
                product.RowId = created.Id;

                _ = this.activityService.AddActivityAsync(new Models.Activity
                {
                    Type = "added",
                    ProductName = product.Name,
                    Source = "app"
                });

                return created;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching products: {ex.Message}");
                return new AppwriteRow();
            }
        }

        public async Task<bool> DeleteProductAsync(string rowId)
        {
            try
            {
                var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{CollectionId}/rows/{rowId}";

                var response = await _http.DeleteAsync(url);

                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching products: {ex.Message}");
                return false;
            }
        }


        public async Task<AppwriteRow?> UpdateProductAsync(Product product)
        {
            try
            {
                var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{CollectionId}/rows/{product.RowId}";

                var body = new
                {
                    data = new
                    {
                        quantity = product.Quantity,
                        category = product.Category
                    }
                };

                var response = await _http.PatchAsJsonAsync(url, body);

                response.EnsureSuccessStatusCode();

                // Deserialize the created row
                var created = await response.Content.ReadFromJsonAsync<AppwriteRow>();

                return created;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching products: {ex.Message}");
                return null;
            }
        }

        public static string GenerateId(int length = 20)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var buffer = new char[length];

            // First char must be alphanumeric (no special chars)
            buffer[0] = chars[random.Next(chars.Length)];

            for (int i = 1; i < length; i++)
                buffer[i] = chars[random.Next(chars.Length)];

            return new string(buffer);
        }

        public class AppwriteCreateRowResponse
        {
            public AppwriteRow Row { get; set; }
        }


        // Matches the new Appwrite Tables API response
        public class AppwriteRowsResponse
        {
            public int Total { get; set; }
            public List<AppwriteRow> Rows { get; set; }
        }

        public class AppwriteRow
        {
            public string Name { get; set; }
            public int Quantity { get; set; }
            public string Category { get; set; }

            [JsonPropertyName("$id")]
            public string Id { get; set; } // maps $id
        }
    }
}