using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OstukorvApp.Data;
using OstukorvApp.Models;

public class CoopScraperService
{
    private readonly AppDbContext _context;

    public CoopScraperService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllProductsAsync(string categoryUrl, int storeId)
    {
        var products = new List<Product>();
        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArguments("--window-size=1920,1080");

        var i = 0;

        using var driver = new ChromeDriver(options);
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        driver.Navigate().GoToUrl(categoryUrl);
        await Task.Delay(500); // lase esilehel laadida

        try
        {
            var cookieButton = wait.Until(d =>
                d.FindElements(By.CssSelector("button.agree-button")).FirstOrDefault(b => b.Displayed));

            if (cookieButton != null)
            {
                cookieButton.Click();
                Console.WriteLine("Cookie-nupp vajutatud.");
                await Task.Delay(500); // lase lehel reageerida
            }
        }
        catch (WebDriverTimeoutException)
        {
            Console.WriteLine("Cookie-nuppu ei leitud - liigume edasi.");
        }


        while (true)
        {
            i++;
            var productCards = driver.FindElements(By.CssSelector("app-product-card"));

            foreach (var card in productCards)
            {
                try
                {
                    var productLink = card.FindElement(By.CssSelector("a.product-content"));
                    string url = productLink.GetAttribute("href");

                    var nameElement = card.FindElement(By.CssSelector(".product-name"));
                    string name = nameElement.Text.Trim();

                    var priceTag = card.FindElement(By.CssSelector("app-price-tag"));
                    var integerPart = priceTag.FindElement(By.CssSelector(".integer")).Text.Trim();
                    var decimalPart = priceTag.FindElement(By.CssSelector(".decimal")).Text.Replace("€", "").Trim();
                    var unitPriceElement = priceTag.FindElements(By.CssSelector(".base")).FirstOrDefault();

                    decimal price = decimal.Parse($"{integerPart}.{decimalPart}", CultureInfo.InvariantCulture);
                    string pricePerUnit = unitPriceElement?.Text ?? "";

                    products.Add(new Product
                    {
                        Name = name,
                        Price = price,
                        PricePerUnit = pricePerUnit.Trim(),
                        Url = url,
                        StoreId = storeId
                    });
                    Console.WriteLine("Tooted loetud" + i);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Toote parsimisel tekkis viga: {ex.Message}");
                }
            }

            try
            {
                wait.Until(d => d.FindElements(By.CssSelector("app-product-card")).Any());

                var nextButton = driver.FindElements(By.CssSelector("a.page-navigation"))
                                       .FirstOrDefault(e => e.Text.Contains("Järgmine"));

                if (nextButton == null || nextButton.GetAttribute("class").Contains("disabled"))
                {
                    Console.WriteLine("Järgmise lehe nupp puudub või on keelatud – lõpetame.");
                    break;
                }

                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", nextButton);
                await Task.Delay(300);

                nextButton.Click();
                await Task.Delay(500); // oota uue lehe laadimist
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lehe vahetusel tekkis viga: {ex.Message}");
                break;
            }
        }

        driver.Quit();
        return products;
    }

    public async Task<List<Product>> GetAndSaveAllProductsAsync(string categoryUrl, int storeId)
    {
        var allProducts = await GetAllProductsAsync(categoryUrl, storeId);

        // Kustuta olemasolevad tooted samast poest (soovi korral)
        var existingProducts = _context.Products.Where(p => p.StoreId == storeId);
        _context.Products.RemoveRange(existingProducts);
        await _context.SaveChangesAsync();

        // Lisa uued
        _context.Products.AddRange(allProducts);
        await _context.SaveChangesAsync();

        return allProducts;
    }

}
