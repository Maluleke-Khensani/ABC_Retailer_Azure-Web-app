using System.Text.Json;
using ABC_Retailers.Azure_Services;
using ABC_Retailers.Data;
using ABC_Retailers.Models;
using ABC_Retailers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABC_Retailers.Controllers
{
    public class ProductCatalogController : Controller
    {

        // Assited by ChatGPT, with heavy modifications

        private readonly IAzureStorageService _azureStorageService;
        private readonly IFunctionsApi _api;
        private readonly RetailersDbContext _db;

        public ProductCatalogController( IAzureStorageService azureStorageService, IFunctionsApi api, RetailersDbContext db )
        {
            _azureStorageService = azureStorageService;
            _api = api;
            _db = db;
        }

        //GET: ProductCatalog/Index (with Search)
        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            // Get all products directly from Azure Table
            var products = await _azureStorageService.GetAllEntitiesAsync<ProductCatalog>();

            // Filter by search query on ProductName
            if (!string.IsNullOrEmpty(search))
            {
                var trimmedSearch = search.Trim();
                products = products
                
    .Where(p => (!string.IsNullOrEmpty(p.ProductName) && p.ProductName.Trim().Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.Description) && p.Description.Trim().Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase)))
    .ToList();


                ViewBag.SearchQuery = search;
            }

            // Group by Category safely
            var grouped = products
                .GroupBy(p => string.IsNullOrWhiteSpace(p.Category) ? "Uncategorized" : p.Category.Trim())
                .ToDictionary(g => g.Key, g => g.ToList());

            // In ProductCatalogController.Index
            var username = User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                // Count items from SQL cart
                var cartCount = await _db.Cart.CountAsync(c => c.CustomerUsername == username);
                ViewBag.CartCount = cartCount; // send to Razor
            }
            else
            {
                ViewBag.CartCount = 0;
            }


            // Set cart count for display
            TempData["CartCount"] = HttpContext.Session.GetInt32("CartCount") ?? 0;

            return View(grouped);
        }




        // GET: ProductCatalog/Create
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAsync()
        {
            // Populate Cart items for logged-in user
            List<CartItemViewModel> viewCartItems = new();
            if (TempData["CartSnapshot"] != null)
            {
                viewCartItems = JsonSerializer.Deserialize<List<CartItemViewModel>>(TempData["CartSnapshot"].ToString()!)!;
            }
            ViewBag.CartItems = viewCartItems;


            var products = await _azureStorageService.GetAllEntitiesAsync<ProductCatalog>();
            ViewBag.Categories = products.Select(p => p.Category).Distinct().ToList();
            return View();
        }

        // POST: ProductCatalog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(ProductCatalog product, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(product);

            // Set Azure Table keys
            product.PartitionKey = product.Category?.Trim() ?? "Uncategorized";
            product.RowKey = Guid.NewGuid().ToString();

            // Handle image upload if provided
            if (imageFile != null && imageFile.Length > 0)
            {
                product.ImageUrl = await _azureStorageService.UploadImageAsync(imageFile, "product-images");
            }

            await _azureStorageService.AddEntityAsync(product);

            TempData["Message"] = "Product added successfully!";
            return RedirectToAction(nameof(Index));
        }



        // Optional: View Details page
        public async Task<IActionResult> Details(string category, string rowKey)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            var product = await _azureStorageService.GetEntityAsync<ProductCatalog>(category, rowKey);
            if (product == null) return NotFound();

            return View(product);
        }


        // GET: ProductCatalog/Edit
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            var product = await _azureStorageService.GetEntityAsync<ProductCatalog>(partitionKey, rowKey);

            if (product == null)
                return NotFound();

            return View(product);
        }


        // POST: ProductCatalog/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey, ProductCatalog product, IFormFile? imageFile)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            // Fetch the original product from Azure Table
            var original = await _azureStorageService.GetEntityAsync<ProductCatalog>(partitionKey, rowKey);
            if (original == null)
                return NotFound();

            // Only update ImageUrl if a new file is uploaded
            if (imageFile != null && imageFile.Length > 0)
            {
                product.ImageUrl = await _api.UploadProductImageAsync(imageFile);
            }
            else
            {
                // Keep existing image URL if no new file is uploaded
                product.ImageUrl = original.ImageUrl;
            }

            // Preserve immutable properties
            product.PartitionKey = original.PartitionKey;
            product.RowKey = original.RowKey;
            product.ETag = Azure.ETag.All;

            // Optional: validate other fields here if needed
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fix the validation errors and try again.";

                // Make sure ImageUrl is populated when returning to view
                return View(product);
            }

            try
            {
                // Update Azure Table
                await _azureStorageService.UpdateEntityAsync(product);
                await _api.SendProductUpdateAsync(product);

                TempData["Message"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating product: {ex.Message}";
                return View(product);
            }
        }



        // GET: ProductCatalog/Delete
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            var product = await _azureStorageService.GetEntityAsync<ProductCatalog>(partitionKey, rowKey);
            if (product == null) return NotFound();

            return View(product);
        }

        // POST: ProductCatalog/DeleteConfirmed
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _azureStorageService.DeleteEntityAsync<ProductCatalog>(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }


    }
}