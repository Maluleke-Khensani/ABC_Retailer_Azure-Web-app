using System.Net;
using ABC_Retails_Functions.HelperClasses;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;

namespace ABC_Retails_Functions.AddProductFunctions;

public class AddProduct
{
    private readonly ILogger<AddProduct> _logger;
    private readonly string _conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
    private readonly string _table = "ProductDetails";

    public AddProduct(ILogger<AddProduct> logger)
    {
        _logger = logger;
    }
    [Function("Products_Create")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] HttpRequestData req)
    {
        try
        {
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation($"Received JSON: {body}");

            if (string.IsNullOrWhiteSpace(body))
                return await MyHttpHelper.Text(req, HttpStatusCode.BadRequest, "Request body is empty.");

            // Deserialize JSON with case-insensitive property matching
            Products? product = JsonSerializer.Deserialize<Products>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            // Validate essential fields
            if (product == null)
                return await MyHttpHelper.Text(req, HttpStatusCode.BadRequest, "Invalid JSON.");

            if (string.IsNullOrWhiteSpace(product.ProductName))
                return await MyHttpHelper.Text(req, HttpStatusCode.BadRequest, "ProductName is required.");

            if (string.IsNullOrWhiteSpace(product.Category))
                return await MyHttpHelper.Text(req, HttpStatusCode.BadRequest, "Category is required.");

            // Setup Azure Table Storage
            var tableClient = new TableClient(_conn, _table);
            await tableClient.CreateIfNotExistsAsync();

            // Ensure unique RowKey
            product.PartitionKey = product.Category;
            if (string.IsNullOrWhiteSpace(product.RowKey))
                product.RowKey = $"PROD-{Guid.NewGuid():N}".Substring(0, 8);

            await tableClient.AddEntityAsync(product);

            _logger.LogInformation($"Product created: {product.RowKey} - {product.ProductName}");

            return await MyHttpHelper.Json(req, HttpStatusCode.Created, product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return await MyHttpHelper.Text(req, HttpStatusCode.InternalServerError, $"Server error: {ex.Message}");
        }
    }
}
