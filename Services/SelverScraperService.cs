using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Threading;
using System.Linq;
using System.Globalization;
using OstukorvApp.Models;
using OpenQA.Selenium.Support.UI;
using OstukorvApp.Data;

namespace OstukorvApp.Services
{
    public class SelverScraperService
    {
        private readonly AppDbContext _context;
        private readonly IWebDriver _driver;

        public SelverScraperService(AppDbContext context)
        {
            _context = context;
            var options = new ChromeOptions();
            options.AddArgument("--headless"); // Peidetud režiim
            _driver = new ChromeDriver(options);
        }

        public async Task<List<string>> GetCategoryLinksAsync(string baseUrl)
        {
            var categoryLinks = new List<string>();

            try
            {
                _driver.Navigate().GoToUrl(baseUrl);
                await Task.Delay(3000);

                Console.WriteLine("Otsin kategoorialinke...");

                var categoryElements = _driver.FindElements(By.CssSelector("li.SidebarMenu__item"));

                foreach (var element in categoryElements)
                {
                    try
                    {
                        // Kontrollime index väärtust
                        string indexValue = element.GetAttribute("index");
                        if (int.TryParse(indexValue, out int index))
                        {
                            // Ainult indeksid vahemikus 6-10 ja 13-18
                            if ((index >= 6 && index <= 10) || (index >= 13 && index <= 18))
                            {
                                var links = element.FindElements(By.CssSelector("a.SidebarMenu__link"));

                                foreach (var link in links)
                                {
                                    string href = link.GetAttribute("href");

                                    if (!string.IsNullOrEmpty(href) && !href.StartsWith("#"))
                                    {
                                        categoryLinks.Add(href.StartsWith("http") ? href : baseUrl + href);
                                        Console.WriteLine($"Lisatud link: {href}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Console.WriteLine("Viga elemendi töötlemisel: " + innerEx.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Viga scrapingus: " + ex.Message);
            }
            finally
            {
                Console.WriteLine("Sulgen ChromeDriveri...");
                _driver.Quit();
            }

            return categoryLinks;
        }

        public async Task<List<Product>> GetProductsFromCategory(string categoryUrl, int storeId)
        {
            var options = new ChromeOptions();
            options.AddArguments("--headless=new");
            options.AddArguments("--window-size=1920,1080");
            using var driver = new ChromeDriver(options);
            driver.Navigate().GoToUrl(categoryUrl);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            await Task.Delay(200);

            var cookieAcceptButton = driver.FindElement(By.Id("CybotCookiebotDialogBodyLevelButtonLevelOptinAllowAll"));
            if (cookieAcceptButton.Displayed)
            {
                cookieAcceptButton.Click();
            }

            await Task.Delay(1000);

            var dropdownTrigger = driver.FindElement(By.CssSelector(".Dropdown.Limiter"));
            if (dropdownTrigger != null)
            {
                dropdownTrigger.Click();
                await Task.Delay(100);

                var option96 = driver.FindElements(By.CssSelector("input[value='96'] + label")).FirstOrDefault();
                if (option96 != null)
                {
                    option96.Click();
                    await Task.Delay(100);
                }
            }
            else
            {
                Console.WriteLine("Dropdown.Limiter elementi ei leitud – liigume edasi vaikimisi toodetega.");
            }

            wait.Until(d => d.FindElements(By.ClassName("ProductCard")).Any());

            var products = new List<Product>();
            var productCards = driver.FindElements(By.CssSelector(".ProductCard"));

            Console.WriteLine($"Leitud {productCards.Count} toodet");

            foreach (var card in productCards)
            {
                try
                {
                    var nameElement = card.FindElement(By.CssSelector(".ProductCard__title a"));
                    var priceElements = card.FindElements(By.CssSelector(".ProductPrice"));

                    string name = nameElement.Text;
                    string url = nameElement.GetAttribute("href");

                    var priceElement = priceElements
                        .FirstOrDefault(e => !e.GetAttribute("class").Contains("ProductPrice--original"));

                    if (priceElement == null) continue;

                    string priceText = priceElement.Text.Split('€')[0].Trim();
                    var unitElement = priceElement.FindElements(By.ClassName("ProductPrice__unit-price")).FirstOrDefault();
                    string unitText = unitElement?.Text ?? "";

                    Console.WriteLine($"Hind tekstina: {priceText}, Ühikuhind: {unitText}");

                    if (decimal.TryParse(priceText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                    {
                        products.Add(new Product
                        {
                            Name = name,
                            Price = price,
                            PricePerUnit = unitText.Trim(),
                            Url = url,
                            StoreId = storeId
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Viga toote töötlemisel: {ex.Message}");
                }
            }

            while (true)
            {
                try
                {
                    var nextPageButton = driver.FindElement(By.CssSelector("a[aria-label='Go to previous next']"));

                    if (nextPageButton.Displayed && nextPageButton.Enabled)
                    {
                        nextPageButton.Click();
                        await Task.Delay(300); // anna aega lehe laadimiseks

                        var newProductCards = driver.FindElements(By.CssSelector(".ProductCard"));
                        foreach (var card in newProductCards)
                        {
                            try
                            {
                                var nameElement = card.FindElement(By.CssSelector(".ProductCard__title a"));
                                var priceElements = card.FindElements(By.CssSelector(".ProductPrice"));

                                string name = nameElement.Text;
                                string url = nameElement.GetAttribute("href");

                                var priceElement = priceElements
                                    .FirstOrDefault(e => !e.GetAttribute("class").Contains("ProductPrice--original"));

                                if (priceElement == null) continue;

                                string priceText = priceElement.Text.Split('€')[0].Trim();
                                var unitElement = priceElement.FindElements(By.ClassName("ProductPrice__unit-price")).FirstOrDefault();
                                string unitText = unitElement?.Text ?? "";

                                Console.WriteLine($"Hind tekstina: {priceText}, Ühikuhind: {unitText}");

                                if (decimal.TryParse(priceText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                                {
                                    products.Add(new Product
                                    {
                                        Name = name,
                                        Price = price,
                                        PricePerUnit = unitText.Trim(),
                                        Url = url,
                                        StoreId = storeId
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Viga toote töötlemisel (järgmine leht): {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        break; // kui pole nähtav/aktiivne, lõpetame
                    }
                }
                catch (NoSuchElementException)
                {
                    Console.WriteLine("Järgmise lehe nuppu ei leitud – lõpetame.");
                    break;
                }
            }


            driver.Quit();
            return products;
        }

        public async Task<List<Product>> GetProductsFromAllCategoriesAsync()
        {
            string baseUrl = "https://www.selver.ee/";
            int storeId = 1;
            var allProducts = new List<Product>();

            var existingProducts = _context.Products.Where(p => p.StoreId == storeId);
            _context.Products.RemoveRange(existingProducts);
            await _context.SaveChangesAsync();

            var categoryUrls = await GetCategoryLinksAsync(baseUrl);

            var seenUrls = new HashSet<string>();

            foreach (var url in categoryUrls)
            {
                var products = await GetProductsFromCategory(url, storeId);

                foreach (var product in products)
                {
                    if (seenUrls.Add(product.Url))
                    {
                        allProducts.Add(product);
                    }
                }
            }

            _context.Products.AddRange(allProducts);
            await _context.SaveChangesAsync();

            return allProducts;
        }
    }
}
