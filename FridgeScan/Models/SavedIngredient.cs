namespace FridgeScan.Models;

public class SavedIngredient
{
    public float? Quantity { get; set; }
    public string? Unit { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Original { get; set; }

    public string DisplayText => Original ?? Name;
}
