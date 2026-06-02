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
    [NotifyPropertyChangedFor(nameof(CircleBackgroundColor))]
    private bool isChecked;

    public TextDecorations TextDecorations
        => IsChecked ? TextDecorations.Strikethrough : TextDecorations.None;

    public double Opacity
        => IsChecked ? 0.5 : 1.0;

    public Color TextColor
        => IsChecked ? Color.FromArgb("#8888AA") : Color.FromArgb("#CCCCDD");

    public Color CircleBackgroundColor
        => IsChecked ? Color.FromArgb("#CCCCDD") : Colors.Transparent;
}
