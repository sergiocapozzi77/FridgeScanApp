using CommunityToolkit.Mvvm.ComponentModel;

namespace FridgeScan.Models;

public partial class IngredientItem : ObservableObject
{
    private readonly string? _rawQuantity;

    public IngredientItem(string name, float? quantity = null, string? unit = null, string? ingredientName = null, string? rawQuantity = null)
    {
        Name = name;
        OriginalQuantity = quantity;
        Unit = unit;
        IngredientName = ingredientName ?? name;
        _rawQuantity = rawQuantity;

        DisplayText = rawQuantity != null
            ? FormatOriginalText(rawQuantity, Unit, IngredientName)
            : quantity.HasValue
                ? FormatAdjustedText(quantity.Value, Unit, IngredientName)
                : name;
    }

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextColor))]
    [NotifyPropertyChangedFor(nameof(Opacity))]
    [NotifyPropertyChangedFor(nameof(TextDecorations))]
    [NotifyPropertyChangedFor(nameof(CheckboxBackground))]
    private bool isChecked;

    [ObservableProperty]
    private string displayText;

    /// <summary>
    /// The parsed numeric quantity (e.g., 200 for "200g pasta").
    /// Null when the quantity could not be parsed from the raw ingredient string.
    /// </summary>
    public float? OriginalQuantity { get; }

    /// <summary>
    /// The parsed unit (e.g., "g", "cups", "tbsp").
    /// Null when no unit was detected.
    /// </summary>
    public string? Unit { get; }

    /// <summary>
    /// The ingredient name without the quantity/unit prefix (e.g., "pasta").
    /// Falls back to the raw Name if no quantity was parsed.
    /// </summary>
    public string IngredientName { get; }

    /// <summary>
    /// Adjusts the displayed quantity by the given ratio and updates DisplayText.
    /// For ingredients without a parseable quantity, DisplayText stays unchanged.
    /// Shows the adjusted value as a decimal (fractions aren't useful after scaling).
    /// </summary>
    public void AdjustQuantity(float ratio)
    {
        if (OriginalQuantity.HasValue)
        {
            var adjusted = OriginalQuantity.Value * ratio;
            DisplayText = FormatAdjustedText(adjusted, Unit, IngredientName);
        }
    }

    /// <summary>
    /// Formats the display using the original quantity string from the recipe
    /// (e.g. "1/4", "½"), preserving fractions exactly as written.
    /// </summary>
    private static string FormatOriginalText(string rawQuantity, string? unit, string name)
    {
        if (string.IsNullOrEmpty(unit))
            return $"{rawQuantity} {name}";

        return $"{rawQuantity} {unit} {name}";
    }

    /// <summary>
    /// Formats the display using a computed decimal quantity.
    /// Used for serving-adjusted values where fractions don't apply.
    /// </summary>
    private static string FormatAdjustedText(float qty, string? unit, string name)
    {
        var qtyStr = qty == Math.Floor(qty) && qty < int.MaxValue
            ? ((int)qty).ToString()
            : qty.ToString("0.##");

        if (string.IsNullOrEmpty(unit))
            return $"{qtyStr} {name}";

        return $"{qtyStr} {unit} {name}";
    }

    public TextDecorations TextDecorations
        => IsChecked ? TextDecorations.Strikethrough : TextDecorations.None;

    public double Opacity
        => IsChecked ? 0.5 : 1.0;

    public string CheckboxBackground => IsChecked ? "#8B7CFF" : "#2A2D50";
    public string TextColor => IsChecked ? "#888888" : "White";
}
