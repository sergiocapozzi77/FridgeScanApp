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

    public class RecipeAiService
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
            string cuisine,
            string dishType)
        {
            var template = @"
You are a recipe generator.

Use the following inputs:
- Ingredients: {ingredients}
- Cuisine: {cuisine}
- Dish type: {dishType}

You must:
- Use only the ingredients from the list above (plus common pantry items like salt, pepper, oil, water).
- Return exactly 5 recipe suggestions.
- For each recipe, generate an image prompt that describes the *visual appearance* of the finished dish in concrete detail (colors, textures, plating, setting). 
- The image prompt must be suitable for searching on Pexels and must NOT mention AI, prompts, or instructions.

Output ONLY valid JSON — no explanations, no text before or after.

Format:
[
  {{
    ""name"": ""Dish name"",
    ""imageprompt"": ""A vivid, photographic description of the dish for Pexels search""
  }}
]
";

            var prompt = PromptTemplate.FromTemplate(template);
            var finalPrompt = await prompt.FormatAsync(new InputValues(new Dictionary<string, object>
            {
                { "ingredients", string.Join(',', ingredients) },
                { "cuisine", cuisine },
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
