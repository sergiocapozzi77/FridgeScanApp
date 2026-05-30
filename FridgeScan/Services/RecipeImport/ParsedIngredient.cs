namespace FridgeScan.Services.RecipeImport;

public class ParsedIngredient
{
    public float? Quantity { get; set; }
    public string? Unit { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
