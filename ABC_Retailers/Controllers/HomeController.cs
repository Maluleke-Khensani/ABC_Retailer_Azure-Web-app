using System.Diagnostics;
using ABC_Retail.Models;
using ABC_Retailers.Azure_Services;
using ABC_Retailers.Models;
using ABCRetailers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    // Assited by ChatGPT, with heavy modifications
    public class HomeController : Controller
    {
        private readonly IAzureStorageService _azureStorageService;

        public HomeController(IAzureStorageService azureStorageService)
        {
            _azureStorageService = azureStorageService;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _azureStorageService.GetAllEntitiesAsync<Customers>();
            var products = await _azureStorageService.GetAllEntitiesAsync<Products>();
            var orders = await _azureStorageService.GetAllEntitiesAsync<Orders>();
            var catalog = await _azureStorageService.GetAllEntitiesAsync<ProductCatalog>();

            // Pick two featured products (Laptop + iPhone) from ProductCatalog
            var featuredProducts = catalog
                .Where(c => c.ProductName.Contains("Laptop", StringComparison.OrdinalIgnoreCase)
                         || c.ProductName.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();

            var model = new HomeViewModel
            {
                CustomerCount = customers.Count,
                ProductCount = products.Count,
                OrderCount = orders.Count,
                FeaturedProducts = featuredProducts  // 👈 now directly from ProductCatalog
            };

            return View(model);
        }


    }


}