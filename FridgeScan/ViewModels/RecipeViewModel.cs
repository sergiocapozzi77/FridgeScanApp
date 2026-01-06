using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Windows.Input;

namespace FridgeScan.ViewModels;

public partial class RecipeViewModel : BaseViewModel
{
    private readonly ProductsManager productsManager;
    private readonly IRecipeService recipeService;

    [ObservableProperty]
    public bool isLoading;

    [ObservableProperty]
    public MealTypeModel selectedMealType;

    public ObservableCollection<MealTypeModel> MealTypes { get; set; }

    public ObservableCollection<RecipeSuggestion> Suggestions { get; } = new();

    public RecipeSuggestion SelectedSuggestion { get; set; }

    public ICommand LoadSuggestionsCommand { get; }
    public ICommand OpenRecipeCommand { get; }


    public RecipeViewModel(ProductsManager productsManager, IRecipeService recipeService)
    {
        this.productsManager = productsManager;
        this.recipeService = recipeService;

        MealTypes = new ObservableCollection<MealTypeModel>
        {
            new() { Label = "Breakfast", Value = "breakfast" },
            new() { Label = "Brunch", Value = "brunch" },
            new() { Label = "Dessert", Value = "dessert" },
            new() { Label = "Dinner", Value = "dinner" },
            new() { Label = "Fish Course", Value = "fish-course" },
            new() { Label = "Lunch", Value = "lunch" },
            new() { Label = "Main course", Value = "main-course" },
            new() { Label = "Pasta", Value = "pasta" },
            new() { Label = "Soup", Value = "soup" },
            new() { Label = "Starter", Value = "starter" },
            new() { Label = "Side", Value = "side" }
        };

        LoadSuggestionsCommand = new Command(async () => await LoadSuggestionsAsync());
        OpenRecipeCommand = new Command<RecipeSuggestion>(async (s) => await OpenRecipeAsync(s));

        // Imposta un default
        selectedMealType = MealTypes.First(x => x.Value == "main-course");

        Task.Run(LoadSuggestionsAsync);
    }

    partial void OnSelectedMealTypeChanged(MealTypeModel value)
    {
        if (value != null)
        {
            // Esegue il refresh automatico quando cambia il filtro
            LoadSuggestionsAsync();
        }
    }

    public async Task LoadSuggestionsAsync()
    {
        IsLoading = true;
        try
        {

            var list = await recipeService.GetRecipeSuggestionsAsync(
                productsManager.Products.Select( x => x.Name).ToList(),
                SelectedMealType.Value
            );


            //list[0].ImageUrl = image;
            var ps = new PexelService();

         /*   foreach (var recipe in list)
            {
                var image = recipe.ImagePrompt;
                var url = await ps.GetFoodImageAsync(image);
                recipe.ImageUrl = url;
            }*/

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
