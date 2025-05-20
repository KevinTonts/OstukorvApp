using Microsoft.AspNetCore.Mvc;
using OstukorvApp.Services;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace OstukorvApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScraperController : ControllerBase
    {
        private readonly SelverScraperService _selverScraperService;
        private readonly CoopScraperService _coopScraperService;

        public ScraperController(SelverScraperService selverScraperService, CoopScraperService coopScraperService)
        {
            _selverScraperService = selverScraperService;
            _coopScraperService = coopScraperService;
        }


        [HttpGet("scrape/selver")]
        public async Task<IActionResult> ScrapeSelver()
        {
            var products = await _selverScraperService.GetProductsFromAllCategoriesAsync();
            return Ok(new { Store = "Selver", Count = products.Count });
        }

        [HttpGet("scrape/coop")]
        public async Task<IActionResult> ScrapeCoop()
        {
            var products = await _coopScraperService.GetAndSaveAllProductsAsync("https://vandra.ecoop.ee/et/tooted", storeId: 2);
            return Ok(new { Store = "Coop", Count = products.Count });
        }


    }
}
