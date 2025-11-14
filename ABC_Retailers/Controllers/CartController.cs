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
    [Authorize]
    public class CartController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly RetailersDbContext _db;


        public CartController(IAzureStorageService storageService, RetailersDbContext db)
        {
            _storageService = storageService;
            _db = db;
        }

        // GET: /Cart
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "ProductCatalog");

            var cartItems = await _storageService.GetCartItemsByUserAsync(username); // SQL Cart

            var cartView = new CartPageView
            {
                Items = new List<CartItemViewModel>()
            };

            foreach (var item in cartItems)
            {
                // Pull product details from ProductCatalog
                var product = (await _storageService.GetAllEntitiesAsync<ProductCatalog>())
                                .FirstOrDefault(p => p.RowKey == item.ProductId);

                if (product != null)
                {
                    cartView.Items.Add(new CartItemViewModel
                    {
                        ProductId = item.ProductId,
                        ProductName = product.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price,
                        Subtotal = item.Quantity * product.Price,
                        ImageUrl = product.ImageUrl
                    });


                }
            }

            return View(cartView);
        }


        // POST: /Cart/Add
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CartAddRequest request)
        {
            if (request == null || request.Quantity <= 0)
                return BadRequest();

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var existingItem = (await _storageService.GetCartItemsByUserAsync(username))
                .FirstOrDefault(c => c.ProductId == request.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
                await _storageService.AddToCartAsync(existingItem); // Update quantity in SQL
            }
            else
            {
                await _storageService.AddToCartAsync(new Cart
                {
                    CustomerUsername = username,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                });
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantities(List<CartItemViewModel> items)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var cartItems = await _storageService.GetCartItemsByUserAsync(username);

            foreach (var item in items)
            {
                var cartItem = cartItems.FirstOrDefault(c => c.ProductId == item.ProductId);
                if (cartItem != null && item.Quantity > 0)
                {
                    // Update quantity directly
                    cartItem.Quantity = item.Quantity;

                    await _db.SaveChangesAsync();

                    // Instead of Add, call UpdateEntity logic
                    _db.Cart.Update(cartItem);
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Index");
        }




        // POST: /Cart/Remove
        [HttpPost]
        public async Task<IActionResult> Remove(string productId)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var cartItems = await _storageService.GetCartItemsByUserAsync(username);
            var item = cartItems.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                _storageService.DeleteCartItem(item.Id);
            }

            return RedirectToAction("Index");
        }

        // POST: /Cart/Checkout
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            // Get cart items
            var cartItems = await _storageService.GetCartItemsByUserAsync(username);
            if (!cartItems.Any())
                return RedirectToAction("Index");

            // Pull product info for each item
            var catalogProducts = await _storageService.GetAllEntitiesAsync<ProductCatalog>();
            var viewCartItems = new List<CartItemViewModel>();
            foreach (var item in cartItems)
            {
                var product = catalogProducts.FirstOrDefault(p => p.RowKey == item.ProductId);
                if (product != null)
                {
                    viewCartItems.Add(new CartItemViewModel
                    {
                        ProductId = item.ProductId,
                        ProductName = product.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price,
                        Subtotal = item.Quantity * product.Price,
                        ImageUrl = product.ImageUrl
                    });
                }
            }

            // Store cart snapshot in TempData for confirm page
            TempData["CartSnapshot"] = JsonSerializer.Serialize(viewCartItems);

            // Place order in Azure Table
            await _storageService.PlaceOrderFromCartAsync(username);

            // Clear SQL cart
            var dbCartItems = await _storageService.GetCartItemsByUserAsync(username);
            _db.Cart.RemoveRange(dbCartItems);
            await _db.SaveChangesAsync();

            // Redirect to confirm page
            return RedirectToAction("Create", "ProofOfPaymentRecord");
        }

    }

    public class CartAddRequest
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
