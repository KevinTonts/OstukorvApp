#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OstukorvApp.Models;

namespace OstukorvApp.Services
{
    public class MealPlannerService
    {
        private readonly RecipeService _recipeService;

        public MealPlannerService()
        {
            _recipeService = new RecipeService();
        }





        public Dictionary<string, Dictionary<string, ShoppingItem>> GenerateShoppingList(Dictionary<string, Dictionary<string, Recipe>> weeklyResults, int people)
        {
            var shoppingListPerDay = new Dictionary<string, Dictionary<string, ShoppingItem>>();

            foreach (var day in weeklyResults.Keys)
            {
                var dailyShoppingList = new Dictionary<string, ShoppingItem>();

                foreach (var mealType in weeklyResults[day].Keys)
                {
                    var recipe = weeklyResults[day][mealType];

                    foreach (var ingredient in recipe.Ingredients)
                    {
                        var name = ingredient.Name.ToLowerInvariant();
                        var quantity = (decimal)ingredient.Quantity * people;
                        var unit = ingredient.Unit;

                        if (!dailyShoppingList.ContainsKey(name))
                        {
                            dailyShoppingList[name] = new ShoppingItem
                            {
                                Quantity = quantity,
                                Unit = unit
                            };
                        }
                        else
                        {
                            dailyShoppingList[name].Quantity += quantity;
                        }
                    }
                }

                shoppingListPerDay[day] = dailyShoppingList;
            }

            return shoppingListPerDay;
        }


        public Dictionary<string, Dictionary<string, Recipe>> PlanWeek(
    decimal totalBudget,
    int people,
    List<string> weekdays,
    Dictionary<string, List<string>> mealTypesByDay)
        {
            var result = new Dictionary<string, Dictionary<string, Recipe>>();
            var allRecipes = _recipeService.GetAllRecipes();
            var usedRecipes = new HashSet<string>();

            int totalMeals = mealTypesByDay.Sum(x => x.Value.Count);
            decimal budgetPerMeal = totalMeals > 0 ? totalBudget / totalMeals : 0;

            foreach (var day in weekdays)
            {
                var mealsForDay = new Dictionary<string, Recipe>();
                if (!mealTypesByDay.TryGetValue(day, out var mealTypes)) continue;

                foreach (var mealType in mealTypes)
                {
                    var candidates = allRecipes
                        .Where(r => r.MealType.Equals(mealType, StringComparison.OrdinalIgnoreCase))
                        .Where(r => !usedRecipes.Contains(r.Name))
                        .OrderBy(r => EstimateCostPerPortion(r))
                        .ToList();

                    foreach (var recipe in candidates)
                    {
                        var totalCost = EstimateCostPerPortion(recipe) * people;
                        if (totalCost <= budgetPerMeal)
                        {
                            mealsForDay[mealType] = recipe;
                            usedRecipes.Add(recipe.Name);
                            break;
                        }
                    }
                }

                result[day] = mealsForDay;
            }

            return result;
        }


        public Dictionary<string, Recipe> PlanSingleDay(
            decimal budget,
            int people,
           List<string> mealTypes)
        {
            var result = new Dictionary<string, Recipe>();
            var allRecipes = _recipeService.GetAllRecipes();
            var usedNames = new HashSet<string>();

            decimal budgetPerMeal = budget / (mealTypes.Count > 0 ? mealTypes.Count : 1);

            foreach (var mealType in mealTypes)
            {
                var candidates = allRecipes
                    .Where(r => r.MealType.ToLowerInvariant() == mealType.ToLowerInvariant())
                    .Where(r => !usedNames.Contains(r.Name))
                    .OrderBy(r => Guid.NewGuid()) // juhuslik valik
                    .ToList();

                foreach (var recipe in candidates)
                {
                    var cost = EstimateCostPerPortion(recipe) * people;
                    if (cost <= budgetPerMeal)
                    {
                        result[mealType] = recipe;
                        usedNames.Add(recipe.Name);
                        break;
                    }
                }
            }

            return result;
        }

        // Demo: lihtne hinnang — kõik koostisosad maksavad 0.01€/g või ml
        public decimal EstimateCostPerPortion(Recipe recipe)
        {
            decimal total = 0;

            foreach (var ingredient in recipe.Ingredients)
            {
                // Hinna modelleerimine: 0.01€ iga ühiku kohta
                total += (decimal)ingredient.Quantity * 0.01m;
            }

            return recipe.Portions == 0 ? total : total / recipe.Portions;
        }
    }
}
