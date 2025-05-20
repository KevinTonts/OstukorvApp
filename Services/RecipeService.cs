using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OstukorvApp.Models;

namespace OstukorvApp.Services
{
    public class RecipeService
    {
        private readonly List<Recipe> _recipes;

        public RecipeService()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "recipes.json");
            var json = File.ReadAllText(path);
            _recipes = JsonSerializer.Deserialize<List<Recipe>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
        }

        public List<Recipe> GetAllRecipes() => _recipes;

        public List<Recipe> GetByMealType(string mealType) =>
            _recipes.Where(r => r.MealType.Equals(mealType, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
