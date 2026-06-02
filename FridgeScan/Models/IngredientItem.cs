using CommunityToolkit.Mvvm.ComponentModel;

namespace FridgeScan.Models;

public partial class IngredientItem : ObservableObject
{
    public IngredientItem(string name)
    {
        Name = name;
    }

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextColor))]
    [NotifyPropertyChangedFor(nameof(Opacity))]
    [NotifyPropertyChangedFor(nameof(TextDecorations))]
    [NotifyPropertyChangedFor(nameof(CheckboxBackground))]
    private bool isChecked;

    public TextDecorations TextDecorations
        => IsChecked ? TextDecorations.Strikethrough : TextDecorations.None;

    public double Opacity
        => IsChecked ? 0.5 : 1.0;
    
    
    public string CheckboxBackground => IsChecked ? "#8B7CFF" : "#2A2D50";
    public string TextColor => IsChecked ? "#888888" : "White";
}
