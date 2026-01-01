using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace FridgeScan.Services
{
    public class ActivityService
    {
        private readonly HttpClient _http;

        private const string Endpoint = "https://fra.cloud.appwrite.io/v1";
        private const string ProjectId = "6954045e003c75c1c3bf";
        private const string DatabaseId = "695404ac0021bf7d9707";
        private const string CollectionId = "activity";

        public ActivityService()
        {
            var apiKey = Secrets.AppWriteApiKey;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("X-Appwrite-Project", ProjectId);
            _http.DefaultRequestHeaders.Add("X-Appwrite-Key", apiKey);
        }

        public async Task<List<Models.Activity>> GetActivitiesAsync(string[]? queries = null)
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

                return response?.Rows ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching products: {ex.Message}");
                return new List<Models.Activity>();
            }
        }

        public async Task<Models.Activity> AddActivityAsync(Models.Activity activity)
        {
            try
            {
                var url = $"{Endpoint}/tablesdb/{DatabaseId}/tables/{CollectionId}/rows";

                var body = new
                {
                    rowId = GenerateId(),
                    data = new
                    {
                        metadata = activity.Metadata,
                        source = activity.Source,
                        product_name = activity.ProductName,
                        type = activity.Type
                    }
                };

                var response = await _http.PostAsJsonAsync(url, body);

                response.EnsureSuccessStatusCode();

                // Deserialize the created row
                var created = await response.Content.ReadFromJsonAsync<Models.Activity>();
                activity.RowId = created.RowId;

                return created;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching products: {ex.Message}");
                return new Models.Activity();
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
            public Models.Activity Row { get; set; }
        }


        // Matches the new Appwrite Tables API response
        public class AppwriteRowsResponse
        {
            public int Total { get; set; }
            public List<Models.Activity> Rows { get; set; }
        }
    }
}