using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FridgeScan.Models
{
    public class RecipeSuggestion
    {
        public string Name { get; set; }

        public string ImagePrompt { get; set; } // returned by GPT-

        public string ImageUrl { get; set; } // returned by GPT-4o
    }

    public class FullRecipe
    {
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public List<string> Ingredients { get; set; }
        public List<string> Steps { get; set; }
    }
}
