using System.IO;
using ABC_Retailers.Azure_Services;
using ABC_Retailers.Models;
using Humanizer;
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

        // GET: FileUpload/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ProofOfPaymentRecord();

            // Get Orders
            var orders = await _azureStorageService.GetAllEntitiesAsync<Orders>();
            model.Orders = orders.Select(o => new SelectListItem
            {
                Text = o.RowKey,
                Value = o.RowKey
            }).ToList();

            // Get Customers
            var customers = await _azureStorageService.GetAllEntitiesAsync<Customers>();
            ViewBag.Customers = new SelectList(customers, "Username", "Username");

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [HttpPost]
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
                    // ✅ Call your Function API to upload
                    await _functionApi.UploadFileToShareAsync(ProofOfPayment);

                    ViewBag.Message = $"File '{ProofOfPayment.FileName}' uploaded successfully!";
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Error: " + ex.Message;
                }
            }

            // 🔄 Repopulate dropdowns before returning View (CRITICAL)
            var orders = await _azureStorageService.GetAllEntitiesAsync<Orders>();
            model.Orders = orders.Select(o => new SelectListItem
            {
                Text = o.RowKey,
                Value = o.RowKey
            }).ToList();

            var customers = await _azureStorageService.GetAllEntitiesAsync<Customers>();
            ViewBag.Customers = new SelectList(customers, "Username", "Username");

            return View(model);
        }
    }
}





