using System.Collections.Concurrent;
using System.Diagnostics;
using ABC_Retail.Models;
using ABC_Retailers.Azure_Services;
using ABC_Retailers.Controllers;
using ABC_Retailers.Models;
using ABC_Retailers.Models.ViewModels;
using ABCRetailers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace ABC_Retailers.Controllers
{
    // Assited by ChatGPT, with heavy modifications
    public class HomeController : Controller
    {
        private readonly IAzureStorageService _azureStorageService;
        private readonly IFunctionsApi _api;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IAzureStorageService azureStorageService, IFunctionsApi functionApi, ILogger<CustomersController> logger)
        {
            _azureStorageService = azureStorageService;
            _api = functionApi;
        }


        public IActionResult Contact()
        {
            return View();
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CustomerDashboard()
        {
            var customers = await _azureStorageService.GetAllEntitiesAsync<Customers>();
            var products = await _azureStorageService.GetAllEntitiesAsync<Products>();
            var orders = await _azureStorageService.GetAllEntitiesAsync<Orders>();
            var catalog = await _azureStorageService.GetAllEntitiesAsync<ProductCatalog>();

            // Pick a few featured or trending products for the dashboard
            var featuredProducts = catalog
                .OrderByDescending(p => p.Price)
                .Take(4)
                .ToList();

            var model = new HomeViewModel
            {
                CustomerCount = customers.Count,
                ProductCount = products.Count,
                OrderCount = orders.Count,
                FeaturedProducts = featuredProducts
            };

            return View(model);
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

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            try
            {
                var customers = await _azureStorageService.GetAllEntitiesAsync<Customers>();
                var orders = await _azureStorageService.GetAllEntitiesAsync<Orders>();
               
                var model = new
                {
                    TotalCustomers = customers.Count,
                    TotalOrders = orders.Count
                };

                ViewBag.AdminSummary = model;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Admin Dashboard data.");
                TempData["Error"] = "Could not load Admin Dashboard data.";
                return View();
            }
        }

        // =======================
        // 🔒 PRIVACY PAGE (Public)
        // =======================
        [AllowAnonymous]
        public IActionResult Privacy() => View();

        // =======================
        // ⚠️ ERROR PAGE
        // =======================
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }


}


