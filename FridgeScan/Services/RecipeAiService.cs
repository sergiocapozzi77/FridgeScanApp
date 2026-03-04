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

        public Task<RecipeSuggestion> GetFullRecipeDetailsAsync(RecipeSuggestion recipe)
        {
            return GetFullRecipeDetailsInternalAsync(recipe);
        }

        private async Task<RecipeSuggestion> GetFullRecipeDetailsInternalAsync(RecipeSuggestion recipe)
        {
            var template = @"
You are a recipe generator.

Given a recipe name and (optionally) a URL, return a JSON object with the following properties only:

{{
  ""name"": ""..."",
  ""ingredients"": [ ... ],
  ""methodSteps"": [ ... ],
  ""prep_time"": ""..."",      // optional, human-friendly
  ""cookTime"": ""..."",      // optional, human-friendly
  ""serving"": ""..."",       // optional
  ""difficulty"": ""easy|medium|hard""
}}

Use the following inputs:
- Recipe name: {name}
- Ingredients: {ingredients}
- URL: {url}
- Dish type: {dishType}
- Difficulty: {difficulty}
- Total time: {totalTime}

If the URL is provided, you may use it as context to infer accurate ingredients and steps. If not, infer a plausible full recipe based on the name and ingredients.

Return only valid JSON with the fields above. Do not include any extra text.

Rules:
- Prefer recipes that use as many of the listed ingredients as possible.
- It is OK if the recipe uses extra ingredients.
";

            var prompt = PromptTemplate.FromTemplate(template);

            var finalPrompt = await prompt.FormatAsync(new InputValues(new Dictionary<string, object>
            {
                { "name", recipe.Name ?? string.Empty },
                { "ingredients", recipe.Ingredients != null ? string.Join(',', recipe.Ingredients) : string.Empty },
                { "url", recipe.Url ?? string.Empty },
                { "dishType", recipe.DishType ?? "any" },
                { "difficulty", recipe.Difficulty ?? "any" },
                { "totalTime", recipe.PrepTime ?? "any" }
            }));

            var result = await llm.GenerateAsync(finalPrompt);

            var output = result.LastMessageContent ?? string.Empty;

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var parsed = JsonSerializer.Deserialize<RecipeSuggestion>(output, options);

                if (parsed != null)
                {
                    // merge fields into provided recipe
                    recipe.Ingredients = parsed.Ingredients ?? new List<string>();
                    recipe.MethodSteps = parsed.MethodSteps ?? new List<string>();
                    recipe.PrepTime = parsed.PrepTime ?? recipe.PrepTime;
                    recipe.CookTime = parsed.CookTime ?? recipe.CookTime;
                    recipe.Serving = parsed.Serving ?? recipe.Serving;
                    recipe.Difficulty = parsed.Difficulty ?? recipe.Difficulty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing recipe details: {ex.Message}");
            }

            return recipe;
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
            string dishType, string[] keywords, string? difficulty, string? totalTime)
        {
            var template = @"
You are a recipe generator.

Use the following inputs:
- Ingredients: {ingredients}
- Dish type: {dishType}
- Keywords: {keywords}
- Difficulty: {difficulty}
- Total time: {totalTime}

Rules:
- Prefer recipes that use as many of the listed ingredients as possible.
- It is OK if the recipe uses extra ingredients.
- Return exactly 5 recipes.
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
                { "dishType", dishType },
                { "keywords", string.Join(',', keywords ) },
                { "difficulty", difficulty ?? "any" },
                { "totalTime", totalTime ?? "any" }
            }));

            var result = await llm.GenerateAsync(finalPrompt);

            var output = result.LastMessageContent;

            var suggetions = JsonSerializer.Deserialize<List<RecipeSuggestion>>(output ?? "", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return (suggetions ?? new List<RecipeSuggestion>()).Select

                (s =>
            {
                s.RecipeSource = "AI";
                s.DishType = dishType;
                return s;
            }).ToList();
            

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
