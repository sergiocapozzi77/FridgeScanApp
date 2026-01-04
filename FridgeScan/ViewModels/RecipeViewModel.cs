using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FridgeScan.ViewModels;

public partial class RecipeViewModel : BaseViewModel
{
    private readonly RecipeAiService _ai;
    private readonly ProductsManager productsManager;

    [ObservableProperty]
    public bool isLoading;

    public ObservableCollection<RecipeSuggestion> Suggestions { get; } = new();

    public RecipeSuggestion SelectedSuggestion { get; set; }

    public ICommand LoadSuggestionsCommand { get; }
    public ICommand OpenRecipeCommand { get; }

    public RecipeViewModel(ProductsManager productsManager)
    {
        this.productsManager = productsManager;

        _ai = new RecipeAiService();

        LoadSuggestionsCommand = new Command(async () => await LoadSuggestionsAsync());
        OpenRecipeCommand = new Command<RecipeSuggestion>(async (s) => await OpenRecipeAsync(s));

        Task.Run(LoadSuggestionsAsync);
    }

    public async Task LoadSuggestionsAsync()
    {
        IsLoading = true;
        try
        {

            var list = await _ai.GetRecipeSuggestionsAsync(
                productsManager.Products.Select( x => x.Name).ToList(),
                cuisine: "Italian",
                dishType: "Main course"
            );


            //list[0].ImageUrl = image;
            var ps = new PexelService();

            foreach (var recipe in list)
            {
                var image = recipe.ImagePrompt;
                var url = await ps.GetFoodImageAsync(image);
                recipe.ImageUrl = url;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Suggestions.Clear();
                foreach (var item in list)
                    Suggestions.Add(item);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading recipes: {ex}");
            await Toast.Make("Failed to load recipe suggestions. " + ex.Message, ToastDuration.Long).Show();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OpenRecipeAsync(RecipeSuggestion suggestion)
    {
        //    var json = await _ai.GetFullRecipeAsync(suggestion.Name);
        //    var full = JsonSerializer.Deserialize<FullRecipe>(json);

        //    await Shell.Current.GoToAsync(nameof(RecipeDetailsPage), true, new Dictionary<string, object>
        //{
        //    { "Recipe", full }
        //});
    }
}
