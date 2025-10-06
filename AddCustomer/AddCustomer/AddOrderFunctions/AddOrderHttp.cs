using System.Net;
using ABC_Retails_Functions.HttpFunction1;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;


namespace ABC_Retails_Functions.AddOrderFunctions
{
    public class AddOrderHttp
    {
        private readonly ILogger<AddOrderHttp> _logger;

        public AddOrderHttp(ILogger<AddOrderHttp> logger)
        {
            _logger = logger;
        }

        [Function("Orders_Create")]
        [QueueOutput("order-notifications", Connection = "AzureWebJobsStorage")]
        public async Task<Orders?> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
        {
            var response = req.CreateResponse();
            var orderRequest = await req.ReadFromJsonAsync<Orders>();

            if (orderRequest == null || string.IsNullOrWhiteSpace(orderRequest.CustomerId) || string.IsNullOrWhiteSpace(orderRequest.ProductId))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("CustomerId and ProductId are required.");
                return null; // nothing added to queue
            }

            // Create a full Orders object to send to the queue
            var order = new Orders
            {
                PartitionKey = orderRequest.CustomerId,
                RowKey = $"ORD-{Guid.NewGuid():N}".Substring(0, 8),  // short unique OrderId
                CustomerId = orderRequest.CustomerId,
                Username = orderRequest.Username,
                ProductId = orderRequest.ProductId,
                ProductName = orderRequest.ProductName,
                OrderDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                Quantity = orderRequest.Quantity,
                UnitPrice = orderRequest.UnitPrice,
                TotalPrice = orderRequest.Quantity * orderRequest.UnitPrice,
                Status = "Pending"
            };

            _logger.LogInformation($"New order queued for {order.Username} - {order.CustomerId}, OrderId: {order.RowKey}");

            // Return fully populated order to queue
            return order;
        }
    }
}