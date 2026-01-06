using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FridgeScan.ViewModels
{
    public partial class RecipeDetailsViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IRecipeService recipeService;
        [ObservableProperty] private RecipeSuggestion recipe;
        [ObservableProperty] private bool isBusy;

        public RecipeDetailsViewModel(IRecipeService recipeService)
        {
            this.recipeService = recipeService;
        }


        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("RecipeUrl"))
            {
                var url = query["RecipeUrl"].ToString();
                await LoadRecipeDetails(url);
            }
        }

        private async Task LoadRecipeDetails(string url)
        {
            IsBusy = true;
            Recipe = await this.recipeService.GetFullRecipeDetailsAsync(url);
            IsBusy = false;
        }
    }
}
