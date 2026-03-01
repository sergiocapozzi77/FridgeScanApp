using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FridgeScan.ViewModels
{
    public partial class RecipeDetailsViewModel : ObservableObject, IQueryAttributable
    {
        private readonly Func<string, IRecipeService> _factory;
        private readonly RecipeAiService recipeAiService;
        [ObservableProperty] private RecipeSuggestion recipe;
        [ObservableProperty] private bool isBusy;

        public RecipeDetailsViewModel(Func<string, IRecipeService> factory, RecipeAiService recipeAiService)
        {
            _factory = factory;
            this.recipeAiService = recipeAiService;
        }


        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("RecipeUrl") && query.ContainsKey("provider") && query.ContainsKey("Recipe"))
            {
                var url = query["RecipeUrl"].ToString();
                var provider = query["provider"].ToString();
                var recipe = query["Recipe"] as RecipeSuggestion;
               
                await LoadRecipeDetails(provider, recipe);
            }
        }

        private async Task LoadRecipeDetails(string provider, RecipeSuggestion recipe)
        {
            IsBusy = true;

            if (provider == "AI")
            {
                Recipe = await recipeAiService.GetFullRecipeDetailsAsync(recipe);
            }
            else
            {
                var recipeService = _factory(provider);
                Recipe = await recipeService.GetFullRecipeDetailsAsync(recipe.Url);
            }
            IsBusy = false;
        }
    }
}
