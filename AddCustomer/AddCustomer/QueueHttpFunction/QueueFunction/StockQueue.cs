using System;
using System.Text.Json;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABC_Retails_Functions.QueueHttpFunction.QueueFunction;

public class StockQueue
{
    private readonly string _tableName = "Notifications";
    private readonly string _storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;

    [Function("NotificationQueueTrigger")]
    public async Task Run(
        [QueueTrigger("stock-updates", Connection = "AzureWebJobsStorage")] string queueMessage,
        FunctionContext context)
    {
        var logger = context.GetLogger("StockQueueTrigger");
        logger.LogInformation($"Received queue message: {queueMessage}");

        NotificationMessage? notificationData;
        try
        {
            notificationData = JsonSerializer.Deserialize<NotificationMessage>(queueMessage);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize queue message");
            return; // skip invalid messages
        }

        if (notificationData == null || string.IsNullOrWhiteSpace(notificationData.ProductName))
        {
            logger.LogWarning("Queue message is missing required fields. Skipping.");
            return;
        }

        try
        {
            var tableClient = new TableClient(_storageConnectionString, _tableName);
            await tableClient.CreateIfNotExistsAsync();

            var notification = new Notification
            {
                PartitionKey = "Notifications",
                RowKey = Guid.NewGuid().ToString(),
                Message = notificationData.Message,
                Category = notificationData.Category,
                ProductName = notificationData.ProductName,
                NewStock = notificationData.NewStock,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await tableClient.AddEntityAsync(notification);
            logger.LogInformation($"Notification stored in table: {notification.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to store notification in Table Storage");
            throw; // ensure retry/poison queue still works
        }
    }
}
