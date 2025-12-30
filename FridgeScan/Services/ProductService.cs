using System.Net.Http.Json;

namespace FridgeScan.Services
{
    public class ProductService
    {
        private readonly HttpClient _http;

        private const string Endpoint = "https://fra.cloud.appwrite.io/v1";
        private const string ProjectId = "6954045e003c75c1c3bf";
        private const string DatabaseId = "695404ac0021bf7d9707";
        private const string CollectionId = "products";
        private const string ApiKey = "standard_be38725166f290995ea6d9e81ecedbf689d687ca94cd2ff05568ab629f7a51f506401b98e9cc750af531e5cee47c8a60ad94144c573adfb148579b1bec2492becf9200618e021b7bc599be841be73f75bfbd6a23e6440cc4ff5496a502af8f5c8fb5e3d04d406d3aa5f5663f80e1c0d2e4da7266e19e1fab5fb9ccf9a0695ad0";

        public ProductService()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("X-Appwrite-Project", ProjectId);
            _http.DefaultRequestHeaders.Add("X-Appwrite-Key", ApiKey);
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{CollectionId}/rows";

                var response = await _http.GetFromJsonAsync<AppwriteRowsResponse>(url);

                return response?.Rows?.Select(r => new Product
                {
                    Name = r.Name,
                    Quantity = r.Quantity,
                    Category = r.Category ?? "Other"
                }).ToList() ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching products: {ex.Message}");
                return new List<Product>();
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

                var created1 = await response.Content.ReadAsStringAsync();
                // Deserialize the created row
                var created = await response.Content.ReadFromJsonAsync<AppwriteRow>();

                return created;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching products: {ex.Message}");
                return new AppwriteRow();
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

            public string Id { get; set; } // maps $id
        }
    }
}