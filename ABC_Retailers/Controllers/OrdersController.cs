using System.Text.Json;
using ABC_Retailers.Azure_Services;
using ABC_Retailers.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retailers.Controllers
{
    // Assited by ChatGPT, with heavy modifications
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

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _azureStorageService.GetAllEntitiesAsync<Orders>();
            return View(orders.OrderByDescending(o => o.OrderDate));
        }

        // GET: Orders/Create
        public async Task<IActionResult> Create()
        {

            var customers = await _azureStorageService.GetAllEntitiesAsync<Customers>();
            var products = await _azureStorageService.GetAllEntitiesAsync<Products>();

            ViewBag.Customers = customers;
            ViewBag.Products = products;

            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Orders order)
        {
            _logger.LogInformation("Order Data: {@Order}", order);

            if (ModelState.IsValid)
            {
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
                }
            }

            TempData["ErrorMessage"] = "Invalid input. Please check and try again.";
            return View(order);
        }


        // GET: Orders/Details
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            var order = await _azureStorageService.GetEntityAsync<Orders>(partitionKey, rowKey);
            if (order == null) return NotFound();

            return View(order);
        }

        // GET: Orders/Edit
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            var order = await _azureStorageService.GetEntityAsync<Orders>(partitionKey, rowKey);
            if (order == null) return NotFound();

            // Load dropdowns
            ViewBag.Customers = await _azureStorageService.GetAllEntitiesAsync<Customers>();
            ViewBag.Products = await _azureStorageService.GetAllEntitiesAsync<Products>();

            return View(order);
        }

        // POST: Orders/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Orders order)
        {
            if (ModelState.IsValid)
            {
                order.PartitionKey = order.CustomerId;
                order.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);
                order.TotalPrice = order.UnitPrice * order.Quantity;
                order.ETag = Azure.ETag.All;

                await _azureStorageService.UpdateEntityAsync(order);

                TempData["Message"] = "Order updated successfully!"; // ✅ success message

                return RedirectToAction(nameof(Index));
            }

            // Reload dropdowns if validation fails
            ViewBag.Customers = await _azureStorageService.GetAllEntitiesAsync<Customers>();
            ViewBag.Products = await _azureStorageService.GetAllEntitiesAsync<Products>();

            TempData["ErrorMessage"] = "Failed to update order. Please check the input."; // ❌ error message

            return View(order);
        }


        // GET: Orders/Delete
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var order = await _azureStorageService.GetEntityAsync<Orders>(partitionKey, rowKey);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _azureStorageService.DeleteEntityAsync<Orders>(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }


        // GET: Orders/ProcessOrders
        public async Task<IActionResult> ProcessOrders()
        {
            // Try to receive a message
            var message = await _azureStorageService.ReceiveMessageAsync("order-notifications");

            if (string.IsNullOrEmpty(message))
            {
                ViewBag.Message = "No messages found in the queue.";
                return View("QueueResult"); // Make a simple view to display messages
            }

            // Deserialize queue message
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(message);
            string partitionKey = data["PartitionKey"];
            string rowKey = data["RowKey"];

            // Retrieve the order
            var order = await _azureStorageService.GetEntityAsync<Orders>(partitionKey, rowKey);

            if (order == null)
            {
                ViewBag.Message = $"Order not found: {partitionKey}, {rowKey}";
                return View("QueueResult");
            }

            // Update status (simulate processing)
            order.Status = Orders.OrderStatus.Processing.ToString();
            await _azureStorageService.UpdateEntityAsync(order);

            ViewBag.Message = $"Order {rowKey} for Customer {partitionKey} processed successfully.";
            return View("QueueResult");
        }


    }
}
