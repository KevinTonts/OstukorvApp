using Microsoft.AspNetCore.Mvc;
using OstukorvApp.Services;
using OstukorvApp.Models;
using System;

[ApiController]
[Route("api/[controller]")]
public class PlanController : ControllerBase
{
    private readonly MealPlannerService _planner;

    public PlanController(MealPlannerService planner)
    {
        _planner = planner;
    }

    [HttpPost("week")]
    public IActionResult PlanWeek([FromBody] PlanRequest request)
    {
        var results = _planner.PlanWeek(
            request.Budget,
            request.People,
            request.Weekdays,
            request.MealTypesByDay
        );

        return Ok(results);
    }


    [HttpPost("shoppinglist")]
    public IActionResult GetShoppingList([FromBody] ShoppingRequest request)
    {
        var shoppingList = _planner.GenerateShoppingList(request.WeeklyResults, request.People);
        return Ok(shoppingList);
    }


    [HttpPost("day")]
    public IActionResult PlanDay([FromBody] PlanSingleDayRequest request)
    {
        var result = _planner.PlanSingleDay(
            request.Budget,
            request.People,
            request.MealTypes
        );

        return Ok(result);
    }
}
