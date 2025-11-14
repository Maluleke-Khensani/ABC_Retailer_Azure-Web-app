using System.Text;
using System.Text.Json;
using ABC_Retailers.Azure_Services;
using ABC_Retailers.Models;
using Azure.Core.Extensions;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


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

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminView()
        {
            var customers = await _azureStorageService.GetAllEntitiesAsync<Customers>();
            return View("AdminView", customers);
        }


            [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyProfile()
        {
            // Get the currently logged-in username (assuming you store it in session)
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Login");
            }

            // Fetch the current customer's record
            var customers = await _azureStorageService.GetAllEntitiesAsync<Customers>();
            var customer = customers.FirstOrDefault(c => c.Username == username);

            if (customer == null)
            {
                TempData["Error"] = "Customer not found.";
                return RedirectToAction("Index", "Home");
            }

            return View("CustomerProfile", customer);
        }



        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> EditProfile()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Login");
            }

            var customers = await _azureStorageService.GetAllEntitiesAsync<Customers>();
            var customer = customers.FirstOrDefault(c => c.Username == username);

            if (customer == null)
            {
                TempData["Error"] = "Customer not found.";
                return RedirectToAction("MyProfile");
            }

            return View("EditProfile", customer);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> EditProfile(Customers customer)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid input. Please check your data.";
                return View(customer);
            }

            try
            {
                // 1️⃣ Fetch the existing entity using the original PartitionKey + RowKey
                var existing = await _azureStorageService.GetEntityAsync<Customers>(customer.PartitionKey, customer.RowKey);

                if (existing == null)
                {
                    TempData["ErrorMessage"] = "Customer not found in the database.";
                    return RedirectToAction(nameof(MyProfile));
                }

                // 2️⃣ Check if CustomerType changed (PartitionKey change)
                if (!string.Equals(existing.CustomerType, customer.CustomerType, StringComparison.Ordinal))
                {
                    // Delete old entity
                    await _azureStorageService.DeleteEntityAsync<Customers>(existing.PartitionKey, existing.RowKey);

                    // Insert new entity with new PartitionKey
                    var newEntity = new Customers
                    {
                        PartitionKey = customer.CustomerType, // new PartitionKey
                        RowKey = existing.RowKey,
                        FirstName = customer.FirstName,
                        LastName = customer.LastName,
                        Username = existing.Username,
                        Email = customer.Email,
                        ShippingAddress = customer.ShippingAddress,
                        CustomerType = customer.CustomerType,
                        Timestamp = existing.Timestamp,
                        ETag = Azure.ETag.All
                    };

                    await _azureStorageService.AddEntityAsync(newEntity);

                    TempData["Message"] = "Profile updated (type changed).";
                    return RedirectToAction(nameof(MyProfile));
                }

                // 3️⃣ Normal update without PartitionKey change
                existing.FirstName = customer.FirstName;
                existing.LastName = customer.LastName;
                existing.Email = customer.Email;
                existing.ShippingAddress = customer.ShippingAddress;
                existing.ETag = Azure.ETag.All; // force overwrite

                await _azureStorageService.UpdateEntityAsync(existing);

                TempData["Message"] = "Profile updated successfully!";
                return RedirectToAction(nameof(MyProfile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer RowKey={RowKey}", customer.RowKey);
                TempData["ErrorMessage"] = "Error updating profile: " + ex.Message;
                return View(customer);
            }
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


        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]

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
