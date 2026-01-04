using System.Text.Json;


namespace FridgeScan.Services;

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

    public async Task<ProductInfo?> GetProductAsync(string barcode)
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

        string? name = product.TryGetProperty("product_name", out var nameProp)
            ? nameProp.GetString()
            : null;


        // Extract category tags
        List<string> tags = new();

        if (product.TryGetProperty("categories_tags", out var tagsProp) &&
            tagsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var tag in tagsProp.EnumerateArray())
            {
                if (tag.ValueKind == JsonValueKind.String)
                    tags.Add(tag.GetString());
            }
        }

        // Map to UK supermarket category
        string ukCategory = MapUkSupermarketCategory(tags);

        // Fallback if no tags found
        if (string.IsNullOrWhiteSpace(ukCategory))
            ukCategory = "Other";


        string? imageUrl = product.TryGetProperty("image_url", out var imgProp)
            ? imgProp.GetString()
            : null;

        string? thumbUrl = product.TryGetProperty("image_thumb_url", out var thumb)
         ? thumb.GetString()
         : null;
        

        return new ProductInfo
        {
            Barcode = barcode,
            Name = name,
            Category = ukCategory,
            ImageUrl = imageUrl,
            ThumbUrl = thumbUrl
        };
    }

    private static string MapUkSupermarketCategory(IEnumerable<string> tags)
    {
        foreach (var raw in tags)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var t = raw.ToLowerInvariant();

            // ---- Fruit & Veg ----
            if (t.Contains("vegetable") || t.Contains("vegetables") ||
                t.Contains("veg") || t.Contains("fruit"))
                return "Fruit & Veg";

            // ---- Meat & Fish ----
            if (t.Contains("meat") || t.Contains("poultry") ||
                t.Contains("beef") || t.Contains("chicken") ||
                t.Contains("fish") || t.Contains("seafood"))
                return "Meat & Fish";

            // ---- Dairy & Eggs ----
            if (t.Contains("dairy") || t.Contains("milk") ||
                t.Contains("cheese") || t.Contains("yogurt") ||
                t.Contains("egg"))
                return "Dairy & Eggs";

            // ---- Bakery ----
            if (t.Contains("bread") || t.Contains("bakery") ||
                t.Contains("pastry"))
                return "Bakery";

            // ---- Frozen ----
            if (t.Contains("frozen"))
                return "Frozen";

            // ---- Drinks ----
            if (t.Contains("beverage") || t.Contains("drink") ||
                t.Contains("juice") || t.Contains("water"))
                return "Drinks";

            // ---- Snacks ----
            if (t.Contains("snack") || t.Contains("crisps") ||
                t.Contains("chocolate") || t.Contains("sweets"))
                return "Snacks";

            // ---- Cereal & Breakfast ----
            if (t.Contains("cereal") || t.Contains("breakfast") ||
                t.Contains("oats"))
                return "Cereal & Breakfast";

            // ---- Tins & Jars ----
            if (t.Contains("canned") || t.Contains("tinned") ||
                t.Contains("jarred"))
                return "Tins & Jars";

            // ---- Pasta, Rice & Grains ----
            if (t.Contains("pasta") || t.Contains("rice") ||
                t.Contains("grain") || t.Contains("noodle"))
                return "Pasta, Rice & Grains";

            // ---- Condiments & Sauces ----
            if (t.Contains("sauce") || t.Contains("condiment") ||
                t.Contains("spread") || t.Contains("spreads"))
                return "Condiments & Sauces";

            // ---- Household ----
            if (t.Contains("household") || t.Contains("cleaning"))
                return "Household";
        }

        return "Other";
    }
}
