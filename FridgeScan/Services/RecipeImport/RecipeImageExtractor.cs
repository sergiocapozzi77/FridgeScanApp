using HtmlAgilityPack;

namespace FridgeScan.Services.RecipeImport;

public class RecipeImageExtractor : IRecipeImageExtractor
{
    private static readonly string[] StepSelectors =
    {
        ".recipe-step-img img",
        ".step-image img",
        ".method-step img",
        ".mntl-sc-block-image img",
        ".structured-ingredients__list-item img",
        ".recipe__steps img",
        ".instructions img",
        ".mntl-sc-block-html img",
        "[class*='instruction'] img",
        "[class*='step'] img:not([class*='thumbnail'])",
        ".directions img",
        "li[class*='instruction'] img",
        "div[class*='instruction'] img",
        "ol[class*='step'] img",
    };

    private static readonly string[] ContentSelectors =
    {
        ".recipe-content img",
        "#recipe img",
        "article img",
        "[itemtype*='Recipe'] img",
    };

    public Task<List<RecipeImage>> ExtractImagesAsync(string html, Uri baseUrl)
    {
        var images = new List<RecipeImage>();
        var seenUrls = new HashSet<string>();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        foreach (var selector in StepSelectors)
        {
            if (TryExtract(doc, selector)) break;
        }

        if (images.Count == 0)
        {
            foreach (var selector in ContentSelectors)
            {
                if (TryExtract(doc, selector)) break;
            }
        }

        return Task.FromResult(images);

        bool TryExtract(HtmlDocument d, string selector)
        {
            try
            {
                var nodes = d.DocumentNode.SelectNodes(selector);
                if (nodes == null) return false;

                foreach (var node in nodes)
                {
                    var src = node.GetAttributeValue("src", null)
                              ?? node.GetAttributeValue("data-src", null)
                              ?? node.GetAttributeValue("data-lazy-src", null)
                              ?? node.GetAttributeValue("data-original", null);

                    if (string.IsNullOrWhiteSpace(src)) continue;
                    if (src.StartsWith("data:")) continue;

                    var wStr = node.GetAttributeValue("width", "0");
                    var hStr = node.GetAttributeValue("height", "0");
                    if (int.TryParse(wStr, out var w) && int.TryParse(hStr, out var h)
                        && w > 0 && w < 100 && h > 0 && h < 100)
                        continue;

                    string fullUrl;
                    try
                    {
                        fullUrl = new Uri(baseUrl, src).AbsoluteUri;
                    }
                    catch
                    {
                        continue;
                    }

                    if (!seenUrls.Add(fullUrl)) continue;

                    images.Add(new RecipeImage
                    {
                        Ref = $"img_{images.Count + 1}",
                        Url = fullUrl
                    });
                }

                return images.Count > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
