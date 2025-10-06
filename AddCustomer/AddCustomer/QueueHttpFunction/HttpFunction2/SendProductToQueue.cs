using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABC_Retails_Functions.QueueHttpFunction.QueueFunction;

namespace ABC_Retails_Functions.QueueHttpFunction.HttpFunction2
{
    public class SendProductToQueue
    {
        private readonly string _queueConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
        private readonly string _queueName = "stock-updates";

        [Function("SendProductToQueue")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "send-to-queue")] HttpRequestData req,
            FunctionContext context)
        {
            var log = context.GetLogger("SendProductToQueue");
            log.LogInformation("SendProductToQueue function started.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"Raw HTTP body length: {requestBody.Length}");
            log.LogInformation($"Raw HTTP body content: {requestBody}");

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Empty request body. JSON required.");
                return badResponse;
            }

            NotificationMessage? notification;
            try
            {
                notification = JsonSerializer.Deserialize<NotificationMessage>(requestBody);
            }
            catch (JsonException ex)
            {
                log.LogError(ex, "Failed to deserialize HTTP request");
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid JSON format.");
                return badResponse;
            }

            if (notification == null || string.IsNullOrWhiteSpace(notification.ProductName))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("ProductName is required.");
                return badResponse;
            }

            try
            {
                var queueClient = new QueueClient(_queueConnection, _queueName);
                await queueClient.CreateIfNotExistsAsync();

                // Convert notification to JSON and Base64 encode
                string messageJson = JsonSerializer.Serialize(notification);
                string base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(messageJson));

                await queueClient.SendMessageAsync(base64Message);
                log.LogInformation($"Notification message sent to queue: {messageJson}");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to send message to Queue Storage");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Failed to send message to queue.");
                return errorResponse;
            }

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync("Notification queued successfully.");
            return response;
        }
    }
}
