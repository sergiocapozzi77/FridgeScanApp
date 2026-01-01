using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FridgeScan.Models;
using FridgeScan.Services;
using Microsoft.Graph;

namespace FridgeScan.ViewModels;

public partial class ImportViewModel : BaseViewModel
{
    private readonly EmailService _emailService;
    private readonly ProductsViewModel _mainViewModel;

    public ImportViewModel(EmailService emailService, ProductsViewModel mainViewModel)
    {
        _emailService = emailService;
        _mainViewModel = mainViewModel;
    }

    public async Task ImportFromEmailsAsync()
    {
        var messages = await _emailService.FetchPurchaseEmailsAsync();
        var products = new List<Product>();

        foreach (var msg in messages)
        {
            var body = msg.Body;
            // Very naive parsing: look for lines like "2 x Apples" or "Apples - 2"
            var lines = body.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var p = ParseProductLine(line);
                if (p != null)
                    products.Add(p);
            }
        }

        //_mainViewModel.AddProducts(products);
    }

    private Product? ParseProductLine(string line)
    {
        // examples: "2 x Apples", "Apples x2", "Apples - 2"
        var trimmed = line.Trim();
        // try pattern: number then x then name
        var parts = trimmed.Split(' ');
        if (parts.Length >= 3 && int.TryParse(parts[0], out var q) && (parts[1].ToLower().Contains("x") || parts[1] == "x"))
        {
            var name = string.Join(' ', parts.Skip(2));
            return new Product(name, null, q);
        }

        // try last token is number
        var last = parts.Last();
        if (int.TryParse(last, out q))
        {
            var name = string.Join(' ', parts.Take(parts.Length - 1));
            return new Product(name, null, q);
        }

        // find tokens like "x2"
        foreach (var token in parts)
        {
            if (token.StartsWith("x") && int.TryParse(token.Substring(1), out q))
            {
                var name = string.Join(' ', parts.Where(p => p != token));
                return new Product(name, null, q);
            }
        }

        return null;
    }
}
