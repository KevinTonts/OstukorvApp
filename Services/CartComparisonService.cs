using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OstukorvApp.Data;
using OstukorvApp.Models;

namespace OstukorvApp.Services
{
    public class CartComparisonService
    {
        private readonly AppDbContext _context;

        public CartComparisonService(AppDbContext context)
        {
            _context = context;
        }

        public Dictionary<string, (decimal TotalCost, decimal FullCost, List<Product> Products, List<string> MissingIngredients)> CompareStores(List<Ingredient> ingredients)
        {
            Console.WriteLine("==== VÕRDLUS ALUSTATUD ====");

            var normalizedIngredients = ingredients.Select(i =>
            {
                var unit = i.Unit.Trim().ToLower();
                double quantity = i.Quantity;

                if (unit == "ml") { quantity /= 1000.0; unit = "l"; }
                else if (unit == "g") { quantity /= 1000.0; unit = "kg"; }

                return new Ingredient { Name = i.Name, Quantity = quantity, Unit = unit };
            }).ToList();

            var groupedByStore = _context.Products
                .Include(p => p.Store)
                .GroupBy(p => p.Store.Name)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new Dictionary<string, (decimal, decimal, List<Product>, List<string>)>();

            foreach (var store in groupedByStore)
            {
                Console.WriteLine($"\n-- Töötlen poodi: {store.Key} --");

                decimal totalUsed = 0;
                decimal totalFull = 0;
                List<Product> selectedProducts = new();
                List<string> missingIngredients = new();

                foreach (var ingredient in normalizedIngredients)
                {
                    Console.WriteLine($"\nKoostisosa: {ingredient.Name}, Kogus: {ingredient.Quantity} {ingredient.Unit}");

                    string effectiveSearchName = ingredient.Name.ToLower();
                    Func<double, int> converter = null;
                    string overrideUnit = ingredient.Unit;

                    if (_ingredientSubstitutions.TryGetValue(effectiveSearchName, out var sub))
                    {
                        effectiveSearchName = sub.ActualIngredient;
                        converter = sub.QuantityConverter;
                        overrideUnit = sub.UnitType;
                    }

                    var candidates = store.Value
                        .Where(p => !IsExcluded(p.Name))
                        .Select(p => new
                        {
                            Product = p,
                            Score = CalculateMatchScore(p.Name, effectiveSearchName),
                            PricePerUnit = ParsePricePerUnit(p.PricePerUnit) ?? p.Price
                        })
                        .Where(x => x.Score > 0)
                        .OrderByDescending(x => x.Score)
                        .ThenBy(x => x.PricePerUnit)
                        .ToList();

                    var bestMatch = candidates
                        .Where(c => c.Score >= 4 && !IsSuspicious(c.Product.Name))
                        .OrderBy(c => c.PricePerUnit)
                        .FirstOrDefault() ?? candidates.FirstOrDefault();

                    if (bestMatch != null)
                    {
                        // → Kontrolli, kas koostisosale on alias (nt sidruni mahl → sidrun)
                        var effectiveName = ingredient.Name.ToLower();
                        if (_ingredientAliases.TryGetValue(effectiveName, out var aliases) && aliases.Count > 0)
                        {
                            effectiveName = aliases[0];
                        }


                        decimal usedPrice;

                        if (effectiveName.Contains("puljong") && ingredient.Unit == "l")
                        {
                            int cubesNeeded = (int)Math.Ceiling(ingredient.Quantity / 0.5);
                            var unitPricePerCube = bestMatch.Product.Price / 12m;
                            usedPrice = unitPricePerCube * cubesNeeded;

                            Console.WriteLine($"   🧂 Puljongikuubikud: {cubesNeeded} tk → {Math.Round(usedPrice, 2)} €");
                        }
                        else if (effectiveName.Contains("sidrun") && ingredient.Unit == "l")
                        {
                            int lemonsNeeded = (int)Math.Ceiling(ingredient.Quantity / 0.03);
                            var avgLemonWeightKg = 0.14;
                            var pricePerKg = bestMatch.PricePerUnit;

                            usedPrice = (decimal)(avgLemonWeightKg * lemonsNeeded) * pricePerKg;

                            Console.WriteLine($"   🍋 Sidrunimahl: eeldab {lemonsNeeded} sidrunit → {Math.Round(usedPrice, 2)} € ({Math.Round(pricePerKg, 2)} €/kg)");
                        }
                        else
                        {
                            usedPrice = bestMatch.PricePerUnit * (decimal)(ingredient.Quantity > 0 ? ingredient.Quantity : 1);
                        }

                        totalUsed += usedPrice;
                        totalFull += bestMatch.Product.Price;

                        selectedProducts.Add(bestMatch.Product);
                        Console.WriteLine($"   ✅ Kasutati: {bestMatch.Product.Name} ({bestMatch.PricePerUnit} €/ühik) → {Math.Round(usedPrice, 2)} €");
                    }
                    else
                    {
                        Console.WriteLine("   ❌ Sobivat toodet ei leitud");
                        missingIngredients.Add(ingredient.Name);
                    }
                }

                if (selectedProducts.Any() || missingIngredients.Any())
                {
                    Console.WriteLine($"✔️ Poodi {store.Key} kasutatud summa: {Math.Round(totalUsed, 2)} €, täistoote hind: {Math.Round(totalFull, 2)} €, puuduvad: {string.Join(", ", missingIngredients)}");
                    result[store.Key] = (Math.Round(totalUsed, 2), Math.Round(totalFull, 2), selectedProducts, missingIngredients);
                }
                else
                {
                    Console.WriteLine($"⚠️ Poodi {store.Key} ei lisatud, kuna ühtegi toodet ei sobinud");
                }
            }

            Console.WriteLine("==== VÕRDLUS LÕPETATUD ====");
            return result;
        }




        private bool IsExcluded(string productName)
        {
            var lower = productName.ToLower();
            string[] excluded = {
        "kastmes", "pastakaste", "dip", "dessert", "kompott",
        "magustoit", "joogijogurt", "sprei", "deodorant",
        "kreem", "šokolaad", "kartulilaastud", "krõps", "linnuluu"
    };
            return excluded.Any(word => lower.Contains(word));
        }


        private bool IsSuspicious(string productName)
        {
            var lower = productName.ToLower();
            string[] suspicious = {
    "maitsega", "määre", "snäkk", "kaste", "täidis", "kreem", "lõhn", "mahl", "õli",
    "jäätis", "sidrunimaitseline", "gaseeritud", "joogivesi", "jäätee", "lauavesi",
    "õlis", "marineeritud", "konserv", "võie", "valmis", "pastöriseeritud", "päevalilleõlis", "kõrvitsakuubikud"
};

            return suspicious.Any(word => lower.Contains(word));
        }

        private int CalculateMatchScore(string productName, string ingredientName)
        {
            var productWords = productName.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var ingredientWords = ingredientName.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int score = 0;
            foreach (var word in ingredientWords)
            {
                if (productWords.Contains(word))
                    score += 3;
                else if (productWords.Any(p => p.Contains(word)))
                    score += 1;
            }

            if (_ingredientAliases.TryGetValue(ingredientName.ToLower(), out var aliases))
            {
                foreach (var alias in aliases)
                {
                    if (productName.ToLower().Contains(alias))
                        score += 3;
                }
            }

            if (productName.ToLower().StartsWith(ingredientName.ToLower()))
                score += 2;

            return score;
        }

        private readonly Dictionary<string, List<string>> _ingredientAliases = new()
        {
            { "purustatud tomatid", new List<string> { "tükeldatud tomatid"} },
            { "sidruni mahl", new List<string> { "sidrun" } },
            { "kõrvits", new List<string> { "muskaatkõrvits" } },
            { "kookospiim", new List<string> { "kookosjook" } },
            // lisa soovi korral veel
        };

        private readonly Dictionary<string, (string ActualIngredient, Func<double, int> QuantityConverter, string UnitType)> _ingredientSubstitutions = new()
{
    { "sidruni mahl", ("sidrun", quantity => (int)Math.Ceiling(quantity / 0.03), "kg") },
    { "puljong", ("puljongikuubik", quantity => (int)Math.Ceiling(quantity / 0.5), "tk") }
};




        public static decimal? ParsePricePerUnit(string pricePerUnit)
        {
            if (string.IsNullOrWhiteSpace(pricePerUnit))
                return null;

            var cleaned = pricePerUnit
                .Replace("€", "")
                .Replace(" / kg", "")
                .Replace(" / l", "")
                .Replace(" / tk", "")
                .Replace(" / ml", "")
                .Replace("/kg", "")
                .Replace("/l", "")
                .Replace("/tk", "")
                .Replace("/ml", "")
                .Trim()
                .Replace(",", ".");

            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;

            return null;
        }
    }
}
