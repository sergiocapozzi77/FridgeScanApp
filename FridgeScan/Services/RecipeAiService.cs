using LangChain;
using LangChain.Prompts;
using LangChain.Providers.OpenAI;

namespace FridgeScan.Services
{
    using LangChain.Prompts;
    using LangChain.Providers;
    using LangChain.Providers.OpenAI;
    using LangChain.Providers.OpenAI.Predefined;
    using LangChain.Schema;
    using OpenAI;
    using OpenAI.Images;

    public class RecipeAiService : IRecipeService
    {
        private readonly OpenAiProvider provider;
        private readonly OpenAiLatestFastChatModel llm;

        public RecipeAiService()
        {
            provider = new OpenAiProvider(Secrets.OpenAiKey);
            llm = new OpenAiLatestFastChatModel(provider);
        }

        //public async Task<string> GenerateDishImageAsync(string imagePrompt)
        //{
        //    // Create the image client
        //    var client = new ImageClient(model: "gpt-image-1", apiKey: Secrets.OpenAiKey);

        //    // Call the image API
        //    var result = await client.GenerateImageAsync(
        //        imagePrompt,
        //        new ImageGenerationOptions
        //        {
        //            Size = GeneratedImageSize.W256xH256
        //        }
        //    );

        //    // Save the file locally
        //    var uri = result.Value;
        //    Console.WriteLine($"Image URL: {uri}");
        //    return uri.ImageUri.ToString();
        //}


        public async Task<List<RecipeSuggestion>> GetRecipeSuggestionsAsync(
            List<string> ingredients,
            string dishType, string? difficulty, string? totalTime)
        {
            var template = @"
You are a recipe generator.

Use the following inputs:
- Ingredients: {ingredients}
- Dish type: {dishType}

Rules:
- Prefer recipes that use as many of the listed ingredients as possible.
- It is OK if the recipe uses extra ingredients.
- Return exactly 5 recipes.
- All recipes must come from https://www.bbcgoodfood.com/. But don't invent links, if the link doesn't exist, post the correct link even if it comes from another website
- You may search the web to find real recipes and links.
- For each recipe, extract the preparation time (in minutes) and difficulty, and normalize difficulty to: ""easy"", ""medium"", or ""hard"".

Output ONLY valid JSON — no explanations, no text before or after.

Format:
[
  {{
    ""name"": ""Dish name"",
    ""url"": ""https://www.bbcgoodfood.com/…"",
    ""prep_time"": 0,
    ""difficulty"": ""easy""
  }}
]";

            var prompt = PromptTemplate.FromTemplate(template);
            var finalPrompt = await prompt.FormatAsync(new InputValues(new Dictionary<string, object>
            {
                { "ingredients", string.Join(',', ingredients) },
                { "dishType", dishType }
            }));

            var result = await llm.GenerateAsync(finalPrompt);

            var output = result.LastMessageContent;

            return JsonSerializer.Deserialize<List<RecipeSuggestion>>(output ?? "", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        //        public async Task<string> GetFullRecipeAsync(string recipeName)
        //        {
        //            var prompt = new PromptTemplate(@"
        //Provide the full recipe for: {{recipeName}}

        //Return JSON:

        //{
        //  ""name"": ""..."",
        //  ""ingredients"": [...],
        //  ""steps"": [...],
        //  ""time_minutes"": 0,
        //  ""difficulty"": ""easy | medium | hard""
        //}
        //");

        //            var chain = prompt | _model;

        //            return await chain.RunAsync(new { recipeName });
        //        }
    }

}
