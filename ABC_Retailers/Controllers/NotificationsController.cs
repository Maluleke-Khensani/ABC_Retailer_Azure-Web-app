using ABC_Retailers.Azure_Services;
using ABC_Retailers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retailers.Controllers
{
    [Authorize(Roles = "Admin,Customer")]
    public class NotificationsController : Controller
    {
        private readonly IAzureStorageService _azureStorageService;
        private readonly IFunctionsApi _api;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            ILogger<NotificationsController> logger,
            IAzureStorageService azureStorageService,
            IFunctionsApi api)
        {
            _azureStorageService = azureStorageService;
            _api = api;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var notifications = await _azureStorageService.GetAllEntitiesAsync<Notifications>();

            // Optional: Show toast if redirected after new notification
            if (TempData["NewNotification"] != null)
                ViewBag.NewNotification = TempData["NewNotification"].ToString();

            return View(notifications.OrderByDescending(n => n.CreatedAt).ToList());
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Notifications notification)
        {
            if (!ModelState.IsValid)
                return View(notification);

            notification.PartitionKey = "Notifications";
            notification.RowKey = Guid.NewGuid().ToString();
            notification.CreatedAt = DateTimeOffset.UtcNow;

            await _azureStorageService.AddEntityAsync(notification);

            TempData["NewNotification"] = $"Notification added: {notification.Message}";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var note = await _azureStorageService.GetEntityAsync<Notifications>(partitionKey, rowKey);
            if (note == null) return NotFound();

            return View(note);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            await _azureStorageService.DeleteEntityAsync<Notifications>(partitionKey, rowKey);
            TempData["NewNotification"] = "Notification deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
