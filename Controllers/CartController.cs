using Microsoft.AspNetCore.Mvc;
using OstukorvApp.Services;
using OstukorvApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;


[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly CartComparisonService _comparisonService;

    public CartController(CartComparisonService comparisonService)
    {
        _comparisonService = comparisonService;
    }

    [HttpPost("evaluate")]
    public IActionResult EvaluateCart([FromBody] Dictionary<string, Dictionary<string, IngredientItem>> weeklyCart, [FromQuery] int people = 1, [FromQuery] decimal budget = 0)
    {
        var allIngredients = new List<Ingredient>();

        foreach (var day in weeklyCart.Values)
        {
            foreach (var item in day.Values)
            {
                allIngredients.Add(new Ingredient
                {
                    Name = item.Name,
                    Quantity = item.Quantity,
                    Unit = item.Unit
                });
            }
        }

        var result = _comparisonService.CompareStores(allIngredients);
        var best = result
            .Where(kv => kv.Value.Products.Any())
            .OrderBy(kv => kv.Value.TotalCost)
            .FirstOrDefault();

        var leftover = budget > 0 && best.Value.TotalCost > 0
            ? Math.Round(budget - best.Value.TotalCost, 2)
            : 0;

        return Ok(new
        {
            Comparison = result.ToDictionary(kv => kv.Key, kv => new
            {
                TotalCost = kv.Value.TotalCost,
                FullCost = kv.Value.FullCost,
                MissingIngredients = kv.Value.MissingIngredients
            }),
            RecommendedStore = best.Key,
            Leftover = leftover
        });
    }

    public class IngredientItem
    {
        public string Name { get; set; } = "";
        public double Quantity { get; set; }
        public string Unit { get; set; } = "";
    }

}
