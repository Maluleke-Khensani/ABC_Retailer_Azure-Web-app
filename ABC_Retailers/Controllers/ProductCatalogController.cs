using ABC_Retailers.Azure_Services;
using ABC_Retailers.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retailers.Controllers
{
    public class ProductCatalogController : Controller
    {

        // Assited by ChatGPT, with heavy modifications

        private readonly IAzureStorageService _azureStorageService;
        private readonly IFunctionsApi _api;

        public ProductCatalogController( IAzureStorageService azureStorageService, IFunctionsApi api)
        {
            _azureStorageService = azureStorageService;
            _api = api;
        }
        // GET: Browse Products
        public async Task<IActionResult> Index(string? search)
        {
            // Get all products from Azure Table Storage
            var products = await _api.GetProductsAsync(search);
            // Normalize categories (trim spaces, unify names)
            foreach (var p in products)
            {
                p.Category = p.Category?.Trim();
                if (p.Category.Equals("iPhones", StringComparison.OrdinalIgnoreCase))
                    p.Category = "iPhone";
            }
            // Group all products by category
            var grouped = products
                .GroupBy(p => p.Category)
                .ToDictionary(g => g.Key, g => g.ToList());

            return View(grouped);
        }


        // GET: ProductCatalog/Create
        [HttpGet]
        public async Task<IActionResult> CreateAsync()
        {
            // Get all categories from your ProductCatalog storage
            var products = await _azureStorageService.GetAllEntitiesAsync<ProductCatalog>();
            var categories = products
                .Select(p => p.Category)
                .Distinct()
                .ToList();

            ViewBag.Categories = categories;

            return View();
        }

        // POST: ProductCatalog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCatalog product, IFormFile imageFile)
        {
            if (!ModelState.IsValid)
                return View(product);

            // Ensure PartitionKey and RowKey
            product.PartitionKey = product.Category;
            product.RowKey = product.ProductName; // bound from form

            // inside Create POST (replace current upload logic)
            if (imageFile != null && imageFile.Length > 0)
            {
                // call the function instead of direct blob client
                string imageUrl = await _api.UploadProductImageAsync(imageFile);
                product.ImageUrl = imageUrl;
            }

            // Save the product to Table Storage
            await _azureStorageService.AddEntityAsync(product);

            // Send product update to Azure Function (queue)
            await _api.SendProductUpdateAsync(product);

            return RedirectToAction(nameof(Index));
        }



        // Optional: View Details page
        public async Task<IActionResult> Details(string category, string productName)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(productName))
                return NotFound();

            var product = await _azureStorageService.GetEntityAsync<ProductCatalog>(category, productName);
            if (product == null) return NotFound();

            return View(product);
        }

        // GET: ProductCatalog/Edit
        public async Task<IActionResult> Edit(string category, string productName)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(productName))
                return NotFound();

            var product = await _azureStorageService.GetEntityAsync<ProductCatalog>(category, productName);
            if (product == null) return NotFound();

            return View(product);
        }

        // POST: ProductCatalog/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductCatalog product, IFormFile imageFile)
        {
            if (!ModelState.IsValid)
                return View(product);

            // Ensure PartitionKey = Category and RowKey = ProductName
            product.PartitionKey = product.Category;
            product.RowKey = product.ProductName;
            product.ETag = Azure.ETag.All;
            // inside Create POST (replace current upload logic)
            if (imageFile != null && imageFile.Length > 0)
            {
                // call the function instead of direct blob client
                string imageUrl = await _api.UploadProductImageAsync(imageFile);
                product.ImageUrl = imageUrl;
            }


            // Update the product in Table Storage
            await _azureStorageService.UpdateEntityAsync(product);

            // Send updated product info to Azure Function (queue)
            await _api.SendProductUpdateAsync(product);

            return RedirectToAction(nameof(Index));
        }


        // GET: ProductCatalog/Delete
        public async Task<IActionResult> Delete(string category, string productName)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(productName))
                return NotFound();

            var product = await _azureStorageService.GetEntityAsync<ProductCatalog>(category, productName);
            if (product == null) return NotFound();

            return View(product);
        }

        // POST: ProductCatalog/DeleteConfirmed
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _azureStorageService.DeleteEntityAsync<ProductCatalog>(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }


    }
}