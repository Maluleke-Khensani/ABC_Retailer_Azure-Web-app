using System.Text.Json;
using ABC_Retailers.Azure_Services;
using ABC_Retailers.Models;
using ABC_Retailers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ABC_Retailers.Controllers
{
    public class ProofOfPaymentRecordController : Controller
    {
        private readonly IAzureStorageService _azureStorageService;
        private readonly IFunctionsApi _functionApi;

        public ProofOfPaymentRecordController(IAzureStorageService azureStorageService, IFunctionsApi functionApi)
        {
            _azureStorageService = azureStorageService;
            _functionApi = functionApi;
        }

        // GET: /ProofOfPaymentRecord/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ProofOfPaymentRecord();

            // Populate Orders dropdown
            var orders = await _azureStorageService.GetAllEntitiesAsync<Orders>();
            model.Orders = orders.Select(o => new SelectListItem
            {
                Text = o.RowKey,
                Value = o.RowKey
            }).ToList();

            // Populate Customers dropdown
            var customers = await _azureStorageService.GetAllEntitiesAsync<Customers>();
            ViewBag.Customers = new SelectList(customers, "Username", "Username");

            // 🪄 Retrieve cart snapshot from TempData
            if (TempData["CartSnapshot"] != null)
            {
                var cartSnapshotJson = TempData["CartSnapshot"].ToString();
                var cartItems = JsonSerializer.Deserialize<List<CartItemViewModel>>(cartSnapshotJson!) ?? new();
                ViewBag.CartItems = cartItems;
            }
            else
            {
                ViewBag.CartItems = new List<CartItemViewModel>();
            }

            return View(model);
        }


        // POST: /ProofOfPaymentRecord/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProofOfPaymentRecord model, IFormFile ProofOfPayment)
        {
            if (ProofOfPayment == null || ProofOfPayment.Length == 0)
            {
                ViewBag.Message = "Please upload a valid proof of payment.";
            }
            else
            {
                try
                {
                    // Upload file to Azure File Share
                    await _functionApi.UploadFileToShareAsync(ProofOfPayment);
                    ViewBag.Message = $"File '{ProofOfPayment.FileName}' uploaded successfully!";
                    return RedirectToAction("Index", "Orders");

                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Error: " + ex.Message;
                }
            }

            // Repopulate Orders & Customers dropdowns (CRITICAL)
            var orders = await _azureStorageService.GetAllEntitiesAsync<Orders>();
            model.Orders = orders.Select(o => new SelectListItem
            {
                Text = o.RowKey,
                Value = o.RowKey
            }).ToList();

            var customers = await _azureStorageService.GetAllEntitiesAsync<Customers>();
            ViewBag.Customers = new SelectList(customers, "Username", "Username");

            // Repopulate CartItems for the Razor page
            var username = User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                var cartItems = await _azureStorageService.GetCartItemsByUserAsync(username);
                var catalogProducts = await _azureStorageService.GetAllEntitiesAsync<ProductCatalog>();

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
                            ImageUrl = product.ImageUrl
                        });
                    }
                }

                ViewBag.CartItems = viewCartItems;
            }

            return View(model);
        }
    }
}
