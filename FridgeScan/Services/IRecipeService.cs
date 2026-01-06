
namespace FridgeScan.Services
{
    public interface IRecipeService
    {
        Task<List<RecipeSuggestion>> GetRecipeSuggestionsAsync(List<string> ingredients, string dishType);
    }
}