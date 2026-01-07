
namespace FridgeScan.Services
{
    public interface IRecipeService
    {
        Task<RecipeSuggestion> GetFullRecipeDetailsAsync(string url);
        Task<List<RecipeSuggestion>> GetRecipeSuggestionsAsync(List<string> ingredients, string dishType, string[] keywords, string? difficulty, string? totalTime);
    }
}