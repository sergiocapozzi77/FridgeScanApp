using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FridgeScan.Models;

public class Cookbook : INotifyPropertyChanged
{
    public string RowId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RecipeCount { get; set; }
    public List<string> PreviewImageUrls { get; set; } = new();

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
