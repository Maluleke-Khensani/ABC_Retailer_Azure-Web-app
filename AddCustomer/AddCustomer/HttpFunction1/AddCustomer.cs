using System;
using System.Net;
using System.Threading.Tasks;
using ABC_Retails_Functions.HelperClasses;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;

namespace ABC_Retails_Functions.HttpFunction1
{
    public class AddCustomer
    {
        private readonly ILogger<AddCustomer> _logger;
        private readonly string _conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private readonly string _table = "CustomerDetails";

        public AddCustomer(ILogger<AddCustomer> logger)
        {
            _logger = logger;
        }

        [Function("Customers_Create")]
        public async Task<HttpResponseData> Create(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers")] HttpRequestData req)
        {
            try
            {
                // Read the JSON body
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation($"Received JSON: {body}");

                if (string.IsNullOrWhiteSpace(body))
                    return await MyHttpHelper.Text(req, HttpStatusCode.BadRequest, "Request body is empty.");

                // Deserialize with case-insensitive option
                Customer? customer = JsonSerializer.Deserialize<Customer>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (customer == null || string.IsNullOrWhiteSpace(customer.FirstName) || string.IsNullOrWhiteSpace(customer.Email))
                {
                    return await MyHttpHelper.Text(req, HttpStatusCode.BadRequest, "FirstName and Email are required.");
                }

                // Connect to Table Storage
                var table = new TableClient(_conn, _table);
                await table.CreateIfNotExistsAsync();

                var entity = new Customer
                {
                    PartitionKey = "Customer",
                    RowKey = $"CUST-{Guid.NewGuid():N}".Substring(0, 8),
                    FirstName = customer.FirstName,
                    LastName = customer.LastName ?? "",
                    Username = customer.Username ?? "",
                    Email = customer.Email,
                    ShippingAddress = customer.ShippingAddress ?? "",
                    CustomerType = customer.CustomerType ?? "Retail"
                };

                await table.AddEntityAsync(entity);

                _logger.LogInformation($"Customer created: {entity.RowKey} - {entity.FirstName} {entity.LastName}");

                return await MyHttpHelper.Json(req, HttpStatusCode.Created, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                return await MyHttpHelper.Text(req, HttpStatusCode.InternalServerError, $"Server error: {ex.Message}");
            }
        }
    }
}
