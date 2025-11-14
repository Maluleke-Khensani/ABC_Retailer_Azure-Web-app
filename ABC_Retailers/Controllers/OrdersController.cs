using System.Text.Json;
using ABC_Retailers.Azure_Services;
using ABC_Retailers.Models;
using ABCRetailers.Models.ViewModels;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retailers.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IAzureStorageService _azureStorageService;
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IAzureStorageService azureStorageService, IFunctionsApi functionsApi, ILogger<OrdersController> logger)
        {
            _azureStorageService = azureStorageService;
            _functionsApi = functionsApi;
            _logger = logger;
        }


        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            var isAdmin = User.IsInRole("Admin");

            var orders = await _azureStorageService.GetAllEntitiesAsync<Orders>();

            if (!isAdmin && !string.IsNullOrEmpty(username))
            {
                orders = orders
                    .Where(o => o.PartitionKey.Equals(username, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();
            }
            else
            {
                orders = orders.OrderByDescending(o => o.OrderDate).ToList();
            }

            // Add friendly short order ID
            foreach (var order in orders)
            {
                order.DisplayOrderId = $"ORD-{order.RowKey.GetHashCode().ToString("00000").TrimStart('-')}";
            }

            return View(orders);
        }





        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageOrders()
        {
            var orders = await _azureStorageService.GetAllEntitiesAsync<Orders>();
            return View("Index", orders.OrderByDescending(o => o.OrderDate));
        }


        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create()
        {
            var username = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Login", "Account");

            // 🛒 Get the user's cart
            var cartItems = await _azureStorageService.GetCartItemsByUserAsync(username);

            // Convert to view models
            var viewModelList = new List<ABC_Retailers.Models.ViewModels.CartItemViewModel>();

            foreach (var item in cartItems)
            {
                // Get product details from Azure Table
                var product = await _azureStorageService.GetProductByIdAsync(item.ProductId);
                if (product != null)
                {
                    viewModelList.Add(new ABC_Retailers.Models.ViewModels.CartItemViewModel
                    {
                        ProductId = item.ProductId,
                        ProductName = product.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = (double)product.Price
                    });
                }
            }

            // Pass cart data to view
            ViewBag.CartItems = viewModelList;

            return View(new ProofOfPaymentRecord());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Orders order)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid order input.";
                return View(order);
            }

            try
            {
                await _functionsApi.CreateOrderAsync(order);
                TempData["Message"] = "Order created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                TempData["ErrorMessage"] = "Failed to create order.";
                return View(order);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitOrder(IFormFile ProofOfPayment, DateTime PaymentDate)
        {
            var username = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

            // 1️⃣ Place orders from the cart
            await _azureStorageService.PlaceOrderFromCartAsync(username);

            // 2️⃣ Fetch the latest order(s) for this user
            var orders = (await _azureStorageService.GetAllEntitiesAsync<Orders>())
                            .Where(o => o.PartitionKey == username)
                            .OrderByDescending(o => o.OrderDate)
                            .Take(1)
                            .ToList();

            // 3️⃣ Upload proof of payment and update orders
            if (ProofOfPayment != null && orders.Any())
            {
                // Upload to File Share
                var uploadedFileName = await _azureStorageService.UploadToFileShareAsync(
                    ProofOfPayment, "contracts", "payments"
                );

                foreach (var order in orders)
                {
                    order.ProofFileName = uploadedFileName ?? "TESTFILE.pdf";
                    order.ETag = Azure.ETag.All;

                    // Update using your service (no direct TableClient)
                    await _azureStorageService.UpdateEntityAsync(order);
                }
            }

            TempData["Message"] = "Order placed successfully!";
            return RedirectToAction("Details", new { partitionKey = username, rowKey = orders.First().RowKey });
        }




        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            var order = await _azureStorageService.GetEntityAsync<Orders>(partitionKey, rowKey);
            if (order == null) return NotFound();

            ViewBag.Customers = await _azureStorageService.GetAllEntitiesAsync<Customers>();
            ViewBag.Products = await _azureStorageService.GetAllEntitiesAsync<Products>();
            return View(order);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Orders order)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid input.";
                return View(order);
            }

            try
            {
                order.PartitionKey = order.CustomerId;
                order.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);
                order.TotalPrice = order.UnitPrice * order.Quantity;
                order.ETag = Azure.ETag.All;

                await _azureStorageService.UpdateEntityAsync(order);
                TempData["Message"] = "Order updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order");
                TempData["ErrorMessage"] = "Failed to update order.";
                return View(order);
            }
        }

        [Authorize]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var order = await _azureStorageService.GetEntityAsync<Orders>(partitionKey, rowKey);
            if (order == null) return NotFound();
            return View(order);
        }



        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            // 1️⃣ Fetch the order
            var order = await _azureStorageService.GetEntityAsync<Orders>(partitionKey, rowKey);
            if (order == null)
                return NotFound();

            // 2️⃣ Check roles
            var isAdmin = User.IsInRole("Admin");
            var username = User.Identity?.Name;

            // 3️⃣ Customers can only delete their own orders
            if (!isAdmin && order.CustomerId != username)
            {
                TempData["ErrorMessage"] = "You can only delete your own orders.";
                return RedirectToAction("Index");
            }

            // 4️⃣ Customers can only delete if status is "Placed"
            if (!isAdmin && order.Status != "Placed")
            {
                TempData["ErrorMessage"] = "You can only delete orders that haven’t been processed yet.";
                return RedirectToAction("Index");
            }

            // 5️⃣ Delete the order
            await _azureStorageService.DeleteEntityAsync<Orders>(partitionKey, rowKey);
            TempData["Message"] = "Order deleted successfully.";

            return RedirectToAction("Index");
        }



        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder(string partitionKey, string rowKey)
        {
            var order = await _azureStorageService.GetEntityAsync<Orders>(partitionKey, rowKey);
            if (order == null) return NotFound();

            if (order.Status == "Placed")
                order.Status = "Processing";
            else if (order.Status == "Processing")
                order.Status = "Processed";

            order.ETag = Azure.ETag.All;
            await _azureStorageService.UpdateEntityAsync(order);

            TempData["Message"] = $"Order {order.DisplayOrderId} status updated to {order.Status}";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            var order = await _azureStorageService.GetEntityAsync<Orders>(partitionKey, rowKey);
            if (order == null) return NotFound();

            // Only allow customer to see their own orders
            if (User.IsInRole("Customer") && User.Identity?.Name != order.CustomerId)
                return Forbid();

            // Dynamically generate the proof URL
            string? proofUrl = string.IsNullOrEmpty(order.ProofFileName)
                ? null
                : Url.Action("DownloadFromFileShare", "Orders", new { fileName = order.ProofFileName }, Request.Scheme);

            var vm = new OrderDetailsViewModel
            {
                Order = order,
                ProofFileName = order.ProofFileName,
                ProofFileUrl = proofUrl
            };

            return View(vm);
        }



        public async Task<IActionResult> DownloadFromFileShare(string fileName)
        {
            var data = await _azureStorageService.DownloadFromFileShareAsync("contracts", fileName, "payments");
            return File(data, "application/octet-stream", fileName);
        }


 [HttpGet]
        [Authorize(Roles = "Admin,Customer")]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _azureStorageService.GetProductByIdAsync(productId);
                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.Stock,
                        productName = product.ProductName
                    });
                }

                return Json(new { success = false, message = "Product not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product price for {ProductId}", productId);
                return Json(new { success = false, message = "Error retrieving product price." });
            }
        }
    }
}
