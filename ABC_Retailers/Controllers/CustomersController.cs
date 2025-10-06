using Azure.Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using ABC_Retailers.Azure_Services;
using ABC_Retailers.Models;
using Humanizer;
using System.Text.Json;
using System.Text;


namespace ABC_Retailers.Controllers
{
    public class CustomersController : Controller
    {
        // it will log the errors and other information to the console or a file based on configuration
        //hepls to track issues and debug the application
        private readonly ILogger<CustomersController> _logger;

        // This service provides methods to interact with Azure Table Storage, Blob Storage, Queues, and File Shares
        //example: _azureStorageService.GetAllEntitiesAsync<Models.Customers>() will fetch all customer records from Azure Table Storage
        private readonly IAzureStorageService _azureStorageService;

        private readonly IFunctionsApi _api;
        public CustomersController(ILogger<CustomersController> logger, IAzureStorageService azureStorageService, IFunctionsApi api)
        {
            _logger = logger;
            _azureStorageService = azureStorageService;
            _api = api;
        }

        

        //GET basically retrieves data from the server to display it in the view
        //Method to display the form for creating a new customer
        // GET: Customers
        // This action method fetches all customer records from Azure Table Storage
        public async Task<IActionResult> Index()
        {
            // Example: Fetch all customers from Azure Table Storage
            var customers = await _azureStorageService.GetAllEntitiesAsync<Customers>();
            return View(customers);
        }

        // GET basically retrieves data from the server to display it in the view
        // Method to display the form for creating a new customer
        public IActionResult Create()
        {
            return View();
        }

        // POST basically sends data to the server to create or update, or delete a resource    
        // send or save the data to the server
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customers customer)
        {
            if (!ModelState.IsValid)
                return View(customer);

            try
            {
                // Call the Azure Function
                await _api.CreateCustomerAsync(customer);

                TempData["Success"] = "Customer created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                return View(customer);
            }
        }



        // Method to display detailed information for a single customer
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (partitionKey == null || rowKey == null)
            {
                _logger.LogWarning("Customer keys are null.");
                return NotFound();
            }

            // Fetch the customer entity from Azure Table Storage using the provided PartitionKey + RowKey
            var customer = await _azureStorageService.GetEntityAsync<Customers>(partitionKey, rowKey);

            if (customer == null)
            {
                _logger.LogWarning("Customer not found: {RowKey}", rowKey);
                return NotFound();
            }

            return View(customer);
        }

        // GET basically retrieves data to populate the form for editing
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (partitionKey == null || rowKey == null) return NotFound();

            // Fetch the customer entity from Azure Table Storage using partitionKey and rowKey
            var customer = await _azureStorageService.GetEntityAsync<Customers>(partitionKey, rowKey);

            if (customer == null) return NotFound();

            _logger.LogInformation("Editing customer: {CustomerId}", customer.RowKey);
            return View(customer);
        }

        // POST method to update an existing customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customers customer)
        {
            if (ModelState.IsValid)
            {
                customer.PartitionKey = customer.CustomerType;
                customer.ETag = Azure.ETag.All;

                await _azureStorageService.UpdateEntityAsync(customer);

                _logger.LogInformation("Customer updated successfully: {CustomerId}", customer.RowKey);

                // Add success message
                TempData["Message"] = "Customer updated successfully!";

                return RedirectToAction(nameof(Index));
            }

            _logger.LogWarning("Invalid customer data provided for update.");

            // Optional: add an error message if validation failed
            TempData["ErrorMessage"] = "Failed to update customer. Please check the input.";

            return View(customer);
        }


        
        // GET method to confirm deletion of a customer
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            if (partitionKey == null || rowKey == null) return NotFound();

            // Fetch the customer entity from Azure Table Storage using partitionKey and rowKey
            var customer = await _azureStorageService.GetEntityAsync<Customers>(partitionKey, rowKey);

            if (customer == null) return NotFound();

            return View(customer);
        }

        // POST method to delete the customer entity
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            // First check if this customer has any linked orders
            var orders = await _azureStorageService.GetAllEntitiesAsync<Orders>();
            bool hasOrders = orders.Any(o => o.CustomerId == rowKey);

            if (hasOrders)
            {
                _logger.LogWarning("Cannot delete customer {CustomerId} because they have orders.", rowKey);

                // You can show a friendly error on the UI
                TempData["ErrorMessage"] = "Customer cannot be deleted because they have associated orders.";
                return RedirectToAction(nameof(Index));
            }

            // Safe to delete
            await _azureStorageService.DeleteEntityAsync<Customers>(partitionKey, rowKey);

            _logger.LogInformation("Customer deleted successfully: {CustomerId}", rowKey);

            return RedirectToAction(nameof(Index));
        }
        
    }
}
