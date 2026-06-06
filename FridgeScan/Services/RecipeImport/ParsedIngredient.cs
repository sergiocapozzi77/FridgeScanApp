namespace FridgeScan.Services.RecipeImport;

public class ParsedIngredient
{
    public float? Quantity { get; set; }
    public string? Unit { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Original { get; set; }

    /// <summary>
    /// The original quantity string as it appeared in the raw ingredient
    /// (e.g. "1/4", "½", "1 1/2", "2"). Used to preserve fraction display.
    /// </summary>
    public string? RawQuantity { get; set; }
}
