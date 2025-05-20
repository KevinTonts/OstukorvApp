using System.Collections.Generic;

namespace OstukorvApp.Models
{
    public class PlanRequest
    {
        public decimal Budget { get; set; }
        public int People { get; set; }
        public List<string> Weekdays { get; set; } = new();
        public Dictionary<string, List<string>> MealTypesByDay { get; set; } = new();// kasutusel day-endpointis
        public List<string> DietaryFilters { get; set; } = new();
    }


    public class ShoppingRequest
    {
        public Dictionary<string, Dictionary<string, Recipe>> WeeklyResults { get; set; } = new();
        public int People { get; set; }
    }


    public class PlanSingleDayRequest
    {
        public decimal Budget { get; set; }
        public int People { get; set; }
        public List<string> MealTypes { get; set; } = new();
        public List<string> DietaryFilters { get; set; } = new();
    }

}
