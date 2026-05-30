namespace FridgeScan.ViewModels;

public partial class SharedRecipeViewModel : BaseViewModel, IQueryAttributable
{
    [ObservableProperty]
    private string sharedUrl = string.Empty;

    [ObservableProperty]
    private string pageTitle = "Import Recipe";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("url", out var url))
        {
            SharedUrl = Uri.UnescapeDataString(url?.ToString() ?? string.Empty);
        }
    }

    [RelayCommand]
    private async Task Close()
    {
        await Shell.Current.GoToAsync("..");
    }
}
