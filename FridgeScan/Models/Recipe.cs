using FridgeScan.Services.RecipeImport;
using Newtonsoft.Json;

namespace FridgeScan.Models
{
    public class RecipeSuggestion
    {
        public string RecipeSource { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        [JsonProperty("prep_time")]
        public string PrepTime { get; set; } // returned by GPT-

        public string Difficulty { get; set; }
        public string ImageUrl { get; internal set; }

        public string Serving { get; set; }
        public string CookTime { get; set; }
        public List<string> Ingredients { get; set; } = new();
        public List<InstructionSection> MethodSteps { get; set; } = new();
        public List<string> Nutritions { get; set; } = new();
        public string DishType { get; internal set; }
    }

}
