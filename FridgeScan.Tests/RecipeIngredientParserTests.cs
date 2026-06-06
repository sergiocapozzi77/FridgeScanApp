using FridgeScan.Services.RecipeImport;
using Xunit;

namespace FridgeScan.Tests;

public class RecipeIngredientParserTests
{
    private readonly RecipeIngredientParser _parser = new();

    private ParsedIngredient ParseSingle(string raw)
    {
        var results = _parser.Parse([raw]);
        return results.Single();
    }

    private void AssertParsed(ParsedIngredient result, float? expectedQty, string? expectedUnit, string expectedName, string? expectedNotes = null)
    {
        Assert.Equal(expectedQty, result.Quantity);
        Assert.Equal(expectedUnit, result.Unit);
        Assert.Equal(expectedName, result.Name);
        if (expectedNotes is not null)
            Assert.Equal(expectedNotes, result.Notes);
    }

    // ── The ingredients that were failing ──────────────────────────

    [Fact]
    public void Parse_GramsWithParenthetical_CorrectlyParsed()
    {
        var r = ParseSingle("210 grams (1 and 1/2 cups) plain flour or all purpose flour");
        AssertParsed(r, 210f, "grams", "plain flour or all purpose flour", "(1 and 1/2 cups)");
    }

    [Fact]
    public void Parse_PlainFlour_Alt_Name()
    {
        // Checks the "or" in the name is preserved
        var r = ParseSingle("210 grams (1 and 1/2 cups) plain flour or all purpose flour");
        Assert.Contains("plain flour", r.Name);
        Assert.Contains("all purpose flour", r.Name);
    }

    [Fact]
    public void Parse_FractionTeaspoon_CorrectlyParsed()
    {
        var r = ParseSingle("1/2 teaspoon baking soda");
        AssertParsed(r, 0.5f, "teaspoon", "baking soda");
    }

    [Fact]
    public void Parse_QuarterTeaspoon_CorrectlyParsed()
    {
        var r = ParseSingle("1/4 teaspoon salt");
        AssertParsed(r, 0.25f, "teaspoon", "salt");
    }

    [Fact]
    public void Parse_GramsParentheticalCasterSugar_CorrectlyParsed()
    {
        var r = ParseSingle("150 grams (3/4 cup) caster sugar or granulated sugar");
        AssertParsed(r, 150f, "grams", "caster sugar or granulated sugar", "(3/4 cup)");
    }

    [Fact]
    public void Parse_CupWithParentheticalNote_CorrectlyParsed()
    {
        var r = ParseSingle("1 cup (approximately 1 large) red apple, peeled and roughly chopped");
        AssertParsed(r, 1f, "cup", "red apple, peeled and roughly chopped", "(approximately 1 large)");
    }

    [Fact]
    public void Parse_IntegerQuantityNoUnit_ParsesQuantity()
    {
        var r = ParseSingle("2 large eggs");
        // "large" is in known units as a size descriptor — it will match as unit
        AssertParsed(r, 2f, "large", "eggs");
    }

    [Fact]
    public void Parse_TeaspoonVanilla_CorrectlyParsed()
    {
        var r = ParseSingle("1 teaspoon vanilla extract");
        AssertParsed(r, 1f, "teaspoon", "vanilla extract");
    }

    [Fact]
    public void Parse_MillilitersParentheticalOil_CorrectlyParsed()
    {
        var r = ParseSingle("120 ml (1/2 cup) vegetable oil or flavourless oil");
        AssertParsed(r, 120f, "ml", "vegetable oil or flavourless oil", "(1/2 cup)");
    }

    [Fact]
    public void Parse_MillilitersGreekYogurt_CorrectlyParsed()
    {
        var r = ParseSingle("120 ml (1/2 cup) Greek yogurt");
        AssertParsed(r, 120f, "ml", "Greek yogurt", "(1/2 cup)");
    }

    [Fact]
    public void Parse_TeaspoonsBakingPowder_CorrectlyParsed()
    {
        var r = ParseSingle("2 teaspoons baking powder");
        AssertParsed(r, 2f, "teaspoons", "baking powder");
    }

    // ── Additional real-world ingredient patterns ──────────────────

    [Fact]
    public void Parse_MixedNumberQuantity_CorrectlyParsed()
    {
        var r = ParseSingle("1 1/2 cups all purpose flour");
        AssertParsed(r, 1.5f, "cups", "all purpose flour");
    }

    [Fact]
    public void Parse_DecimalQuantity_CorrectlyParsed()
    {
        var r = ParseSingle("0.5 cup milk");
        AssertParsed(r, 0.5f, "cup", "milk");
    }

    [Fact]
    public void Parse_AffixedUnitAtEnd_SingleWord()
    {
        // AffixedQuantityRegex matches when the entire string is just qty+unit
        var r = ParseSingle("200g");
        AssertParsed(r, 200f, "g", "");
    }

    [Fact]
    public void Parse_AffixedUnitWithFollowingText_NowParsed()
    {
        // "200g pasta" — now parsed via the leading-affixed-unit fallback:
        // digits "200" followed by known unit "g" (no space) then text "pasta"
        var r = ParseSingle("200g pasta");
        AssertParsed(r, 200f, "g", "pasta");
    }

    [Fact]
    public void Parse_UnicodeFraction()
    {
        var r = ParseSingle("½ cup sugar");
        AssertParsed(r, 0.5f, "cup", "sugar");
    }

    [Fact]
    public void Parse_UnicodeFractionAttached()
    {
        var r = ParseSingle("1½ cups all purpose flour");
        AssertParsed(r, 1.5f, "cups", "all purpose flour");
    }

    [Fact]
    public void Parse_NoQuantity_ReturnsFullString()
    {
        var r = ParseSingle("salt to taste");
        AssertParsed(r, null, null, "salt to taste");
    }

    [Fact]
    public void Parse_OnlyIngredientName()
    {
        var r = ParseSingle("fresh basil");
        AssertParsed(r, null, null, "fresh basil");
    }

    [Fact]
    public void Parse_Ounces_CorrectlyParsed()
    {
        var r = ParseSingle("8 oz cream cheese");
        AssertParsed(r, 8f, "oz", "cream cheese");
    }

    [Fact]
    public void Parse_Pounds_CorrectlyParsed()
    {
        var r = ParseSingle("1 lb ground beef");
        AssertParsed(r, 1f, "lb", "ground beef");
    }

    [Fact]
    public void Parse_Tablespoons_CorrectlyParsed()
    {
        var r = ParseSingle("3 tbsp olive oil");
        AssertParsed(r, 3f, "tbsp", "olive oil");
    }

    [Fact]
    public void Parse_Cloves_CorrectlyParsed()
    {
        var r = ParseSingle("4 cloves garlic, minced");
        AssertParsed(r, 4f, "cloves", "garlic, minced");
    }

    [Fact]
    public void Parse_Kilograms_CorrectlyParsed()
    {
        var r = ParseSingle("1 kg potatoes");
        AssertParsed(r, 1f, "kg", "potatoes");
    }

    [Fact]
    public void Parse_Pieces_CorrectlyParsed()
    {
        var r = ParseSingle("6 pcs chicken thighs");
        AssertParsed(r, 6f, "pcs", "chicken thighs");
    }

    [Fact]
    public void Parse_LargeWithUnit_CorrectlyParsed()
    {
        var r = ParseSingle("3 large onions, diced");
        AssertParsed(r, 3f, "large", "onions, diced");
    }

    [Fact]
    public void Parse_SmallWithUnit_CorrectlyParsed()
    {
        var r = ParseSingle("1 small zucchini, sliced");
        AssertParsed(r, 1f, "small", "zucchini, sliced");
    }

    // ── Batch parse test ───────────────────────────────────────────

    [Fact]
    public void Parse_BatchOfIngredients_AllParseCorrectly()
    {
        var raw = new List<string>
        {
            "210 grams (1 and 1/2 cups) plain flour or all purpose flour",
            "2 teaspoons baking powder",
            "1/2 teaspoon baking soda",
            "1/4 teaspoon salt",
            "150 grams (3/4 cup) caster sugar or granulated sugar",
            "1 cup (approximately 1 large) red apple, peeled and roughly chopped",
            "2 large eggs",
            "1 teaspoon vanilla extract",
            "120 ml (1/2 cup) vegetable oil or flavourless oil",
            "120 ml (1/2 cup) Greek yogurt",
        };

        var results = _parser.Parse(raw);

        Assert.Equal(10, results.Count);
        AssertParsed(results[0], 210f, "grams", "plain flour or all purpose flour", "(1 and 1/2 cups)");
        AssertParsed(results[1], 2f, "teaspoons", "baking powder");
        AssertParsed(results[2], 0.5f, "teaspoon", "baking soda");
        AssertParsed(results[3], 0.25f, "teaspoon", "salt");
        AssertParsed(results[4], 150f, "grams", "caster sugar or granulated sugar", "(3/4 cup)");
        AssertParsed(results[5], 1f, "cup", "red apple, peeled and roughly chopped", "(approximately 1 large)");
        // "2 large eggs" — "large" is now a known unit/descriptor
        AssertParsed(results[6], 2f, "large", "eggs");
        AssertParsed(results[7], 1f, "teaspoon", "vanilla extract");
        AssertParsed(results[8], 120f, "ml", "vegetable oil or flavourless oil", "(1/2 cup)");
        AssertParsed(results[9], 120f, "ml", "Greek yogurt", "(1/2 cup)");
    }

    // ── Edge cases ─────────────────────────────────────────────────

    [Fact]
    public void Parse_EmptyInput_ReturnsEmpty()
    {
        var results = _parser.Parse(new List<string>());
        Assert.Empty(results);
    }

    [Fact]
    public void Parse_NullWhitespaceInput_Skipped()
    {
        var results = _parser.Parse(["", null!, "   "]);
        Assert.Empty(results);
    }

    [Fact]
    public void Parse_CommaAsDecimal_CorrectlyParsed()
    {
        var r = ParseSingle("1,5 cups flour");
        AssertParsed(r, 1.5f, "cups", "flour");
    }

    [Fact]
    public void Parse_OriginalText_SetCorrectly()
    {
        var r = ParseSingle("2 cups chopped onions");
        Assert.Equal("2 cups chopped onions", r.Original);
    }

    [Fact]
    public void Parse_GramSingular_CorrectlyParsed()
    {
        var r = ParseSingle("100 gram butter");
        AssertParsed(r, 100f, "gram", "butter");
    }

    [Fact]
    public void Parse_MilliliterPlural_CorrectlyParsed()
    {
        var r = ParseSingle("500 milliliters water");
        AssertParsed(r, 500f, "milliliters", "water");
    }

    [Fact]
    public void Parse_NoSpaceAfterUnit_CorrectlyParsed()
    {
        var r = ParseSingle("1 cup sugar");
        AssertParsed(r, 1f, "cup", "sugar");
    }

    [Fact]
    public void Parse_MultipleSpaces_NotNormalized()
    {
        // Internal whitespace in the name is preserved (not our job to normalize)
        var r = ParseSingle("2   cups   chopped   onions");
        AssertParsed(r, 2f, "cups", "chopped   onions");
    }

    [Fact]
    public void Parse_QuarterCup_CorrectlyParsed()
    {
        // Unicode fraction
        var r = ParseSingle("¼ cup milk");
        AssertParsed(r, 0.25f, "cup", "milk");
    }

    [Fact]
    public void Parse_Pinch_CorrectlyParsed()
    {
        var r = ParseSingle("1 pinch saffron");
        AssertParsed(r, 1f, "pinch", "saffron");
    }

    // ── Italian recipe format (name qty unit) ──────────────────────

    [Fact]
    public void Parse_Italian_Farina00_250g()
    {
        var r = ParseSingle("Farina 00 250 g");
        AssertParsed(r, 250f, "g", "Farina 00");
    }

    [Fact]
    public void Parse_Italian_Strutto_50g()
    {
        var r = ParseSingle("Strutto 50 g");
        AssertParsed(r, 50f, "g", "Strutto");
    }

    [Fact]
    public void Parse_Italian_Burro_50g()
    {
        var r = ParseSingle("Burro 50 g");
        AssertParsed(r, 50f, "g", "Burro");
    }

    [Fact]
    public void Parse_Italian_Zucchero_80g()
    {
        var r = ParseSingle("Zucchero 80 g");
        AssertParsed(r, 80f, "g", "Zucchero");
    }

    [Fact]
    public void Parse_Italian_Miele_20g()
    {
        var r = ParseSingle("Miele millefiori 20 g");
        AssertParsed(r, 20f, "g", "Miele millefiori");
    }

    [Fact]
    public void Parse_Italian_UovaWithParenthetical()
    {
        var r = ParseSingle("Uova (circa 1 medio) 60 g");
        AssertParsed(r, 60f, "g", "Uova (circa 1 medio)");
    }

    [Fact]
    public void Parse_Italian_LatteIntero_40g()
    {
        var r = ParseSingle("Latte intero 40 g");
        AssertParsed(r, 40f, "g", "Latte intero");
    }

    [Fact]
    public void Parse_Italian_ScorzaLimone_Half()
    {
        var r = ParseSingle("Scorza di limone ½");
        AssertParsed(r, 0.5f, null, "Scorza di limone");
    }

    [Fact]
    public void Parse_Italian_ScorzaArancia_Half()
    {
        var r = ParseSingle("Scorza d'arancia ½");
        AssertParsed(r, 0.5f, null, "Scorza d'arancia");
    }

    [Fact]
    public void Parse_Italian_Pizzico()
    {
        var r = ParseSingle("Sale fino 1 pizzico");
        AssertParsed(r, 1f, "pizzico", "Sale fino");
    }

    [Fact]
    public void Parse_Italian_GranoCotto_200g()
    {
        var r = ParseSingle("Grano cotto 200 g");
        AssertParsed(r, 200f, "g", "Grano cotto");
    }

    [Fact]
    public void Parse_Italian_LatteIntero_80g()
    {
        var r = ParseSingle("Latte intero 80 g");
        AssertParsed(r, 80f, "g", "Latte intero");
    }

    [Fact]
    public void Parse_Italian_Burro_25g()
    {
        var r = ParseSingle("Burro 25 g");
        AssertParsed(r, 25f, "g", "Burro");
    }

    [Fact]
    public void Parse_Italian_Ricotta_200g()
    {
        var r = ParseSingle("Ricotta di pecora 200 g");
        AssertParsed(r, 200f, "g", "Ricotta di pecora");
    }

    [Fact]
    public void Parse_Italian_Zucchero_180g()
    {
        var r = ParseSingle("Zucchero 180 g");
        AssertParsed(r, 180f, "g", "Zucchero");
    }

    [Fact]
    public void Parse_Italian_CedroCandito_50g()
    {
        var r = ParseSingle("Cedro candito 50 g");
        AssertParsed(r, 50f, "g", "Cedro candito");
    }

    [Fact]
    public void Parse_Italian_Uova_2_BareNumber()
    {
        var r = ParseSingle("Uova 2");
        AssertParsed(r, 2f, null, "Uova");
    }

    [Fact]
    public void Parse_Italian_Tuorli_1_BareNumber()
    {
        var r = ParseSingle("Tuorli 1");
        AssertParsed(r, 1f, null, "Tuorli");
    }

    [Fact]
    public void Parse_Italian_QB_ReturnsFullString()
    {
        // "q.b." (quanto basta / to taste) — not parseable as qty, returns full string
        var r = ParseSingle("Acqua di fiori d'arancio q.b.");
        AssertParsed(r, null, null, "Acqua di fiori d'arancio q.b.");
    }

    [Fact]
    public void Parse_Italian_LatteIntero_20g()
    {
        var r = ParseSingle("Latte intero 20 g");
        AssertParsed(r, 20f, "g", "Latte intero");
    }

    [Fact]
    public void Parse_Italian_ZuccheroAVelo_QB()
    {
        var r = ParseSingle("Zucchero a velo q.b.");
        AssertParsed(r, null, null, "Zucchero a velo q.b.");
    }

    [Fact]
    public void Parse_Italian_AllIngredients_Batch()
    {
        var raw = new List<string>
        {
            "Farina 00 250 g",
            "Strutto 50 g",
            "Burro 50 g",
            "Zucchero 80 g",
            "Miele millefiori 20 g",
            "Uova (circa 1 medio) 60 g",
            "Latte intero 40 g",
            "Scorza di limone ½",
            "Scorza d'arancia ½",
            "Sale fino 1 pizzico",
            "Grano cotto 200 g",
            "Latte intero 80 g",
            "Burro 25 g",
            "Scorza di limone q.b.",
            "Scorza d'arancia q.b.",
            "Sale fino 1 pizzico",
            "Ricotta di pecora 200 g",
            "Zucchero 180 g",
            "Cedro candito 50 g",
            "Miele millefiori 20 g",
            "Uova 2",
            "Tuorli 1",
            "Acqua di fiori d'arancio q.b.",
            "Latte intero 20 g",
            "Scorza d'arancia q.b.",
            "Scorza di limone q.b.",
            "Zucchero a velo q.b.",
        };

        var results = _parser.Parse(raw);
        Assert.Equal(27, results.Count);

        // Spot-check key ones
        AssertParsed(results[0], 250f, "g", "Farina 00");
        AssertParsed(results[1], 50f, "g", "Strutto");
        AssertParsed(results[7], 0.5f, null, "Scorza di limone");
        AssertParsed(results[9], 1f, "pizzico", "Sale fino");
        AssertParsed(results[20], 2f, null, "Uova");
        AssertParsed(results[21], 1f, null, "Tuorli");
        // q.b. entries return full string as name
        AssertParsed(results[13], null, null, "Scorza di limone q.b.");
        AssertParsed(results[26], null, null, "Zucchero a velo q.b.");
    }

    // ── Modern English recipe format ───────────────────────────────

    [Fact]
    public void Parse_AffixedLeadingGramsWithTrailingText()
    {
        var r = ParseSingle("325g butter at room temperature, plus extra for the tins");
        AssertParsed(r, 325f, "g", "butter at room temperature, plus extra for the tins");
    }

    [Fact]
    public void Parse_AffixedLeadingCasterSugar()
    {
        var r = ParseSingle("425g caster sugar");
        AssertParsed(r, 425f, "g", "caster sugar");
    }

    [Fact]
    public void Parse_TeaspoonAlmondExtract()
    {
        var r = ParseSingle("2 tsp almond extract");
        AssertParsed(r, 2f, "tsp", "almond extract");
    }

    [Fact]
    public void Parse_NoUnitAfterUnicodeFraction()
    {
        // "½ lemon" — no unit follows the quantity, but qty should still be kept
        var r = ParseSingle("½ lemon finely zested");
        AssertParsed(r, 0.5f, null, "lemon finely zested");
    }

    [Fact]
    public void Parse_AffixedLeadingGramsPlainYogurt()
    {
        var r = ParseSingle("250g plain yogurt");
        AssertParsed(r, 250f, "g", "plain yogurt");
    }

    [Fact]
    public void Parse_LargeUnitInMiddleOfName()
    {
        var r = ParseSingle("4 large eggs at room temperature");
        AssertParsed(r, 4f, "large", "eggs at room temperature");
    }

    [Fact]
    public void Parse_AffixedLeadingSelfRaisingFlour()
    {
        var r = ParseSingle("375g self-raising flour");
        AssertParsed(r, 375f, "g", "self-raising flour");
    }

    [Fact]
    public void Parse_AffixedLeadingGroundAlmonds()
    {
        var r = ParseSingle("225g ground almonds");
        AssertParsed(r, 225f, "g", "ground almonds");
    }

    [Fact]
    public void Parse_AffixedLeadingMascarpone()
    {
        var r = ParseSingle("200g mascarpone");
        AssertParsed(r, 200f, "g", "mascarpone");
    }

    [Fact]
    public void Parse_AffixedLeadingDoubleCream()
    {
        var r = ParseSingle("300ml double cream");
        AssertParsed(r, 300f, "ml", "double cream");
    }

    [Fact]
    public void Parse_TablespoonLemonJuice()
    {
        var r = ParseSingle("1 tbsp lemon juice");
        AssertParsed(r, 1f, "tbsp", "lemon juice");
    }

    [Fact]
    public void Parse_NoQuantityAtStart_FullString()
    {
        var r = ParseSingle("icing sugar to taste, plus extra for dusting");
        AssertParsed(r, null, null, "icing sugar to taste, plus extra for dusting");
    }

    [Fact]
    public void Parse_RangeQuantity_UsesFirstValue()
    {
        // "1-2 tsp" — range uses the first number
        var r = ParseSingle("1-2 tsp rose geranium water or rose water, to taste (see tip, below)");
        AssertParsed(r, 1f, "tsp", "rose geranium water or rose water, to taste (see tip, below)");
    }

    [Fact]
    public void Parse_BerriesWithList()
    {
        var r = ParseSingle("800g summer berries (raspberries, blackberries, redcurrants, blackcurrants, hulled strawberries and loganberries)");
        AssertParsed(r, 800f, "g", "summer berries (raspberries, blackberries, redcurrants, blackcurrants, hulled strawberries and loganberries)");
    }

    [Fact]
    public void Parse_TablespoonCasterSugar()
    {
        var r = ParseSingle("4 tbsp caster sugar");
        AssertParsed(r, 4f, "tbsp", "caster sugar");
    }

    [Fact]
    public void Parse_NoQuantityEdibleFlowers()
    {
        var r = ParseSingle("rose geranium flowers or other edible flowers");
        AssertParsed(r, null, null, "rose geranium flowers or other edible flowers");
    }

    [Fact]
    public void Parse_AllEnglishIngredients_Batch()
    {
        var raw = new List<string>
        {
            "325g butter at room temperature, plus extra for the tins",
            "425g caster sugar",
            "2 tsp almond extract",
            "½ lemon finely zested",
            "250g plain yogurt",
            "4 large eggs at room temperature",
            "375g self-raising flour",
            "225g ground almonds",
            "200g mascarpone",
            "300ml double cream",
            "1 tbsp lemon juice",
            "icing sugar to taste, plus extra for dusting",
            "1-2 tsp rose geranium water or rose water, to taste (see tip, below)",
            "800g summer berries (raspberries, blackberries, redcurrants, blackcurrants, hulled strawberries and loganberries)",
            "4 tbsp caster sugar",
            "rose geranium flowers or other edible flowers",
        };

        var results = _parser.Parse(raw);
        Assert.Equal(16, results.Count);

        AssertParsed(results[0], 325f, "g", "butter at room temperature, plus extra for the tins");
        AssertParsed(results[1], 425f, "g", "caster sugar");
        AssertParsed(results[2], 2f, "tsp", "almond extract");
        AssertParsed(results[3], 0.5f, null, "lemon finely zested");
        AssertParsed(results[4], 250f, "g", "plain yogurt");
        AssertParsed(results[5], 4f, "large", "eggs at room temperature");
        AssertParsed(results[6], 375f, "g", "self-raising flour");
        AssertParsed(results[7], 225f, "g", "ground almonds");
        AssertParsed(results[8], 200f, "g", "mascarpone");
        AssertParsed(results[9], 300f, "ml", "double cream");
        AssertParsed(results[10], 1f, "tbsp", "lemon juice");
        AssertParsed(results[11], null, null, "icing sugar to taste, plus extra for dusting");
        AssertParsed(results[12], 1f, "tsp", "rose geranium water or rose water, to taste (see tip, below)");
        AssertParsed(results[13], 800f, "g", "summer berries (raspberries, blackberries, redcurrants, blackcurrants, hulled strawberries and loganberries)");
        AssertParsed(results[14], 4f, "tbsp", "caster sugar");
        AssertParsed(results[15], null, null, "rose geranium flowers or other edible flowers");
    }
}
