using ABC_Retailers.Azure_Services;
using ABC_Retailers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retailers.Controllers
{

    // Assited by ChatGPT, with heavy modifications
    public class ProductsController : Controller
    {
        private readonly ILogger<ProductsController> _logger;
        private readonly IAzureStorageService _azureStorageService;
        private readonly IFunctionsApi _functionsApi;
        public ProductsController(ILogger<ProductsController> logger, IAzureStorageService azureStorageService,IFunctionsApi functionsApi)
        {
            _logger = logger;
            _azureStorageService = azureStorageService;
            _functionsApi = functionsApi;
        }

        // Allow browsing
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var products = await _azureStorageService.GetAllEntitiesAsync<Products>();
            return View(products);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddToCart(string productId, int quantity)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            // Create a new Cart entry for SQL DB
            var cart = new Cart
            {
                CustomerUsername = username,
                ProductId = productId,
                Quantity = quantity
            };

            // Save to Azure SQL via your SQL service
            await _azureStorageService.AddToCartAsync(cart);

            TempData["Message"] = "Product added to cart!";
            return RedirectToAction("Cart", "Cart");
        }



        //  Only Admin can Create a product
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            // Fetch all products once to populate Product dropdowns via JS
            var catalog = await _azureStorageService.GetAllEntitiesAsync<ProductCatalog>();

            // Serialize the catalog to JSON for the JS to use
            ViewBag.CatalogJson = System.Text.Json.JsonSerializer.Serialize(
                catalog.Select(p => new { p.Category, p.ProductName, p.Stock, p.Price }).ToList()
            );

            return View();
        }



        // Only Admin can POST a product
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Products product)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            try
            {
                // Call FunctionsApi to create the product
                await _functionsApi.CreateProductAsync(product);

                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create product via function");
                ModelState.AddModelError("", $"Failed to create product: {ex.Message}");
                return View(product);
            }
        }







        // GET: Products/GetProductsByCategory
        public async Task<IActionResult> GetProductsByCategory(string category)
        {
            var catalog = await _azureStorageService.GetAllEntitiesAsync<ProductCatalog>();

            var products = catalog
                .Where(p => p.Category == category)
                .Select(p => new
                {
                    productName = p.ProductName,
                    stock = p.Stock,
                    price = p.Price
                })
                .ToList();

            return Json(products);
        }


        //Only Admin can Edit a product
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (partitionKey == null || rowKey == null) return NotFound();

            var product = await _azureStorageService.GetEntityAsync<Products>(partitionKey, rowKey);

            if (product == null) return NotFound();

            // Load categories again for dropdown
            var catalog = await _azureStorageService.GetAllEntitiesAsync<ProductCatalog>();
            ViewBag.Categories = catalog.Select(c => c.Category).Distinct().ToList() ?? new List<string>();

            return View(product);
        }



        // POST: Products/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Products product, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                product.PartitionKey = product.Category;

                
                product.ETag = Azure.ETag.All;

                await _azureStorageService.UpdateEntityAsync(product);

                _logger.LogInformation("Product updated successfully: {ProductId}", product.RowKey);

                // Add success message
                TempData["Message"] = "Product updated successfully!";

                return RedirectToAction(nameof(Index));
            }

            var catalog = await _azureStorageService.GetAllEntitiesAsync<ProductCatalog>();
            ViewBag.Categories = catalog.Select(c => c.Category).Distinct().ToList() ?? new List<string>();

            _logger.LogWarning("Invalid product data provided for update.");

            // Add error message
            TempData["ErrorMessage"] = "Failed to update product. Please check the input.";

            return View(product);
        }



        // GET: Products/Details
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (partitionKey == null || rowKey == null) return NotFound();

            var product = await _azureStorageService.GetEntityAsync<Products>(partitionKey, rowKey);

            if (product == null) return NotFound();

            return View(product);
        }

        // GET: Products/Delete
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            if (partitionKey == null || rowKey == null) return NotFound();

            var product = await _azureStorageService.GetEntityAsync<Products>(partitionKey, rowKey);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            // Check if this product is used in any orders
            var orders = await _azureStorageService.GetAllEntitiesAsync<Orders>();
            bool isUsedInOrders = orders.Any(o => o.ProductId == rowKey);

            if (isUsedInOrders)
            {
                _logger.LogWarning("Cannot delete product {ProductId} because it is used in orders.", rowKey);

                // Show friendly error message on UI
                TempData["ErrorMessage"] = "Product cannot be deleted because it is associated with existing orders.";
                return RedirectToAction(nameof(Index));
            }

            // Safe to delete product
            await _azureStorageService.DeleteEntityAsync<Products>(partitionKey, rowKey);

            return RedirectToAction(nameof(Index));
        }




        // GET: Products/GetProductCatalogInfo
        public async Task<IActionResult> GetProductCatalogInfo(string productName)
        {
            if (string.IsNullOrEmpty(productName))
                return Json(null);

            // Get the product from ProductCatalog table
            var catalogItems = await _azureStorageService.GetAllEntitiesAsync<ProductCatalog>();
            var product = catalogItems.FirstOrDefault(p => p.ProductName == productName);

            if (product == null) return Json(null);

            return Json(new { stock = product.Stock, price = product.Price });
        }

    }
}   



        

