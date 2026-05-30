namespace FridgeScan.Services.RecipeImport;

public interface IRecipeIngredientParser
{
    List<ParsedIngredient> Parse(List<string> rawIngredients);
}
