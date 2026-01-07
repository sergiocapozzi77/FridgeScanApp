using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public List<string> MethodSteps { get; set; } = new();
        public List<string> Nutritions { get; set; } = new();

    }

    public class FullRecipe
    {
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public List<string> Ingredients { get; set; }
        public List<string> Steps { get; set; }
    }
}
