using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Windows.Input;

namespace FridgeScan.ViewModels;

public partial class RecipeViewModel : BaseViewModel
{
    private const string MealTypeKey = "selected_meal_type";
    private const string DifficultyKey = "selected_difficulty";
    private const string TotalTimeKey = "selected_total_time";

    private readonly ProductsManager productsManager;
    private readonly IEnumerable<IRecipeService> recipeServices;

    public ObservableCollection<Product> AvailableIngredients { get; }
    public ObservableCollection<Product> SelectedIngredients { get; }


    [ObservableProperty]
    public bool isLoading;

    [ObservableProperty]
    public MealTypeModel selectedMealType;

    public ObservableCollection<MealTypeModel> MealTypes { get; set; }

    public ObservableCollection<RecipeSuggestion> Suggestions { get; } = new();

    [ObservableProperty]
    private ObservableCollection<MealTypeModel> difficulties;

    [ObservableProperty]
    private MealTypeModel selectedDifficulty;

    [ObservableProperty]
    private ObservableCollection<MealTypeModel> totalTimes;

    [ObservableProperty]
    private MealTypeModel selectedTotalTime;


    public RecipeSuggestion SelectedSuggestion { get; set; }

    public ICommand LoadSuggestionsCommand { get; }


    public RecipeViewModel(ProductsManager productsManager, IEnumerable<IRecipeService> recipeServices)
    {
        this.productsManager = productsManager;
        this.recipeServices = recipeServices;

        AvailableIngredients = new ObservableCollection<Product>(productsManager.Products);
        SelectedIngredients = new ObservableCollection<Product>(productsManager.Products);

        // 1. Inizializza le collezioni (come già facevi)
        InitializeFilterCollections();

        // 2. Carica le preferenze salvate
        LoadSavedFilters();

        LoadSuggestionsCommand = new Command(async () => await LoadSuggestionsAsync());
    }

    private void InitializeFilterCollections()
    {
        Difficulties = new ObservableCollection<MealTypeModel>
    {
        new () { Label = "Easy", Value = "easy" },
        new () { Label = "More Effort", Value = "more-effort" },
        new() { Label = "A Challenge", Value = "a-challenge" }
    };

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

        TotalTimes = new ObservableCollection<MealTypeModel>
    {
        new () { Label = "Under 15 mins", Value = "lt-900" },
        new() { Label = "Under 30 mins", Value = "lt-1800" },
        new() { Label = "Under 45 mins", Value = "lt-2700" },
        new() { Label = "Under 1 hour", Value = "lt-3600" },
        new() { Label = "1 hour or more", Value = "gte-3600" }
    };
    }

    private void LoadSavedFilters()
    {
        // Recupera i valori stringa. Se non esistono, usa il default.
        var savedMeal = Preferences.Get(MealTypeKey, "main-course");
        var savedDiff = Preferences.Get(DifficultyKey, null);
        var savedTime = Preferences.Get(TotalTimeKey, null);

        // Assegna l'oggetto corrispondente cercando nelle liste
        selectedMealType = MealTypes.FirstOrDefault(x => x.Value == savedMeal);
        selectedDifficulty = Difficulties.FirstOrDefault(x => x.Value == savedDiff);
        selectedTotalTime = TotalTimes.FirstOrDefault(x => x.Value == savedTime);
    }

    // 3. Salvataggio nei metodi partial (Triggerati da [ObservableProperty])

    partial void OnSelectedMealTypeChanged(MealTypeModel value)
    {
        if (value != null)
        {
            Preferences.Set(MealTypeKey, value.Value);
            _ = LoadSuggestionsAsync();
        }
    }

    partial void OnSelectedDifficultyChanged(MealTypeModel value)
    {
        // Salviamo il valore (anche se null)
        Preferences.Set(DifficultyKey, value?.Value);
        _ = LoadSuggestionsAsync();
    }

    partial void OnSelectedTotalTimeChanged(MealTypeModel value)
    {
        Preferences.Set(TotalTimeKey, value?.Value);
        _ = LoadSuggestionsAsync();
    }

    public async Task LoadSuggestionsAsync()
    {
        if(SelectedIngredients.Count == 0)
        {
            await Toast.Make("Please add some ingredients to get recipe suggestions.", ToastDuration.Long).Show();
            return;
        }

        if (SelectedMealType.Value == null)
        {
            await Toast.Make("Please select a mealt type to get recipe suggestions.", ToastDuration.Long).Show();
            return;
        }

        IsLoading = true;
        try
        {
            var pageTasks = new List<Task<List<RecipeSuggestion>>>();

            foreach (var service in recipeServices)
            {
                pageTasks.Add(
                    service.GetRecipeSuggestionsAsync(SelectedIngredients.Select(x => x.Name).ToList(),
                        SelectedMealType.Value,
                        SelectedDifficulty?.Value,
                        SelectedTotalTime?.Value)
                    );

            }

            // Attendiamo il completamento di entrambi
            var results = await Task.WhenAll(pageTasks);

            //list[0].ImageUrl = image;
            //var ps = new PexelService();

            /*   foreach (var recipe in list)
               {
                   var image = recipe.ImagePrompt;
                   var url = await ps.GetFoodImageAsync(image);
                   recipe.ImageUrl = url;
               }*/

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Suggestions.Clear();
                foreach (var item in results.SelectMany( x => x))
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

    [RelayCommand]
    private async Task OpenRecipe(RecipeSuggestion selectedRecipe)
    {
        if (selectedRecipe == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "RecipeUrl", selectedRecipe.Url },
            { "provider", selectedRecipe.RecipeSource } 
        };

        await Shell.Current.GoToAsync(nameof(RecipeDetailsPage), navigationParameter);
    }

}
