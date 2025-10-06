using System;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Owin.Logging;


namespace ABC_Retails_Functions.AddOrderFunctions;

public class OrdersQueueFunction
{

    private readonly ILogger<OrdersQueueFunction> _logger;
    private readonly TableServiceClient _tableServiceClient;

    public OrdersQueueFunction(ILogger<OrdersQueueFunction> logger, TableServiceClient tableServiceClient)
    {
        _logger = logger;
        _tableServiceClient = tableServiceClient;
    }

    [Function("OrdersQueueProcessor")]
    public async Task Run(
        [QueueTrigger("order-notifications", Connection = "AzureWebJobsStorage")] Orders order)
    {
        if (order == null)
        {
            _logger.LogError("Queue message was null. Skipping processing.");
            return;
        }

        try
        {
            _logger.LogInformation($"Processing new order: {order.RowKey} for {order.ProductName}");

            // Get or create the Orders table
            var tableClient = _tableServiceClient.GetTableClient("Orders");
            await tableClient.CreateIfNotExistsAsync();

            // Update status and timestamp
            order.Status = "Processing";
            order.Timestamp = DateTimeOffset.UtcNow;

            // Save order to Table Storage
            await tableClient.AddEntityAsync(order);

            _logger.LogInformation($"Order {order.RowKey} saved successfully to Table Storage.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving order {order.RowKey}: {ex.Message}");
            throw;
        }
    }
}


