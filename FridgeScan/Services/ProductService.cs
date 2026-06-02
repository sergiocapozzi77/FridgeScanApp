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

                List<string> baseEncodedQueries = new();

                if (queries is { Length: > 0 })
                {
                    baseEncodedQueries.AddRange(queries
                        .Select((q, index) => $"queries[{index}]={Uri.EscapeDataString(q)}"));
                }

                var allRows = new List<AppwriteRow>();

                // Use a reasonable page size for Appwrite; adjust if needed
                const int perPage = 100;
                int offset = 0;
                int total = int.MaxValue;

                while (allRows.Count < total)
                {
                    // Build per-request queries[] entries including limit and offset as JSON
                    var encoded = new List<string>(baseEncodedQueries);

                    var nextIndex = encoded.Count;
                    var limitJson = $"{{\"method\":\"limit\",\"values\":[{perPage}]}}";
                    var offsetJson = $"{{\"method\":\"offset\",\"values\":[{offset}]}}";

                    encoded.Add($"queries[{nextIndex}]={Uri.EscapeDataString(limitJson)}");
                    encoded.Add($"queries[{nextIndex + 1}]={Uri.EscapeDataString(offsetJson)}");

                    var queryString = "?" + string.Join("&", encoded);

                    var url = baseUrl + queryString;

                    var response = await _http.GetFromJsonAsync<AppwriteRowsResponse>(url);

                    if (response == null || response.Rows == null || response.Rows.Count == 0)
                    {
                        // nothing more to fetch or an error occurred
                        break;
                    }

                    if (total == int.MaxValue)
                    {
                        total = response.Total;
                    }

                    allRows.AddRange(response.Rows);

                    // advance offset
                    offset = allRows.Count;

                    // safety: if API returns fewer rows than requested but total is unknown, break to avoid infinite loop
                    if (allRows.Count >= total)
                        break;
                }

                return allRows.Select(r =>
                {
                    DateTime? expiry = null;
                    if (!string.IsNullOrEmpty(r.Expiry) && DateTime.TryParse(r.Expiry, out var parsed))
                        expiry = parsed;

                    return new Product(r.Name, r.Category, r.Quantity)
                    {
                        RowId = r.Id,
                        ExpiryDate = expiry,
                        IsFrozen = r.Frozen
                    };
                }).ToList();
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
                        category = product.Category,
                        expiry = product.ExpiryDate?.ToString("o"),
                        frozen = product.IsFrozen
                    }
                };

                var response = await _http.PostAsJsonAsync(url, body);

                response.EnsureSuccessStatusCode();

                // Deserialize the created row
                var created = await response.Content.ReadFromJsonAsync<AppwriteRow>();
                product.RowId = created.Id;

                await this.activityService.AddActivityAsync(new Models.Activity
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
            if (string.IsNullOrEmpty(rowId))
            {
                Console.WriteLine("Error deleting product: RowId is null or empty");
                return false;
            }

            try
            {
                var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{CollectionId}/rows/{Uri.EscapeDataString(rowId)}";

                var response = await _http.DeleteAsync(url);

                // Appwrite Tables API returns 200 on success, 204 on no-content success
                if (response.StatusCode == System.Net.HttpStatusCode.OK ||
                    response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return true;
                }

                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error deleting product {rowId}: {(int?)ex.StatusCode} {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting product {rowId}: {ex.Message}");
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
                        name = product.Name,
                        quantity = product.Quantity,
                        category = product.Category,
                        expiry = product.ExpiryDate?.ToString("o"),
                        frozen = product.IsFrozen
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
            public string Expiry { get; set; }
            public bool Frozen { get; set; }

            [JsonPropertyName("$id")]
            public string Id { get; set; } // maps $id
        }
    }
}