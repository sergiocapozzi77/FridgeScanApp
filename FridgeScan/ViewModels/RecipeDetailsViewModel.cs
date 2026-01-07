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

        [ObservableProperty] private RecipeSuggestion recipe;
        [ObservableProperty] private bool isBusy;

        public RecipeDetailsViewModel(Func<string, IRecipeService> factory)
        {
            _factory = factory;
        }


        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("RecipeUrl") && query.ContainsKey("provider"))
            {
                var url = query["RecipeUrl"].ToString();
                var provider = query["provider"].ToString();
                await LoadRecipeDetails(provider, url);
            }
        }

        private async Task LoadRecipeDetails(string provider, string url)
        {
            IsBusy = true;

            var recipeService = _factory(provider);

            Recipe = await recipeService.GetFullRecipeDetailsAsync(url);
            IsBusy = false;
        }
    }
}
