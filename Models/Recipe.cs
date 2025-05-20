#nullable enable
using System.Collections.Generic;

namespace OstukorvApp.Models
{
    public class Recipe
    {
        public string Name { get; set; } = "";
        public List<Ingredient> Ingredients { get; set; } = new();
        public string MealType { get; set; } = ""; // Breakfast, Lunch, Dinner
        public int Portions { get; set; }
        public string? Instructions { get; set; }
    }

    public class Ingredient
    {
        public string Name { get; set; } = "";
        public double Quantity { get; set; }
        public string Unit { get; set; } = "";
    }
}
