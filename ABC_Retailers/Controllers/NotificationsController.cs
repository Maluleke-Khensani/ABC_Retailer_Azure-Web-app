using ABC_Retailers.Azure_Services;
using ABC_Retailers.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retailers.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly IAzureStorageService _azureStorageService;

        private readonly IFunctionsApi _api;
        public NotificationsController(ILogger<CustomersController> logger, IAzureStorageService azureStorageService, IFunctionsApi api)
        {
           
            _azureStorageService = azureStorageService;
            _api = api;
        }
        public async Task<IActionResult> Index()
        {
            var notification = await _azureStorageService.GetAllEntitiesAsync<Notifications>();
            return View(notification);
        }

       
    }
}
