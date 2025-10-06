using System.Net;
using ABC_Retails_Functions.HelperClasses;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;


namespace ABC_Retails_Functions.HttpFunction3;

public class SearchFunction
{
    private readonly ILogger<SearchFunction> _logger;

    public SearchFunction(ILogger<SearchFunction> logger)
    {
        _logger = logger;
    }

    [Function("Products_Get")]
    public async Task<HttpResponseData> GetProducts(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req)
    {
        // Optional: read a query parameter for search/filter
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query).Get("q")?.Trim();

        var table = new TableClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "ProductCatalog");
        await table.CreateIfNotExistsAsync();

        // Fetch all products
        var allProducts = table.Query<ProductCatalog>().ToList();

        // Filter if a search query was provided
        if (!string.IsNullOrEmpty(query))
        {
            allProducts = allProducts
                .Where(p => p.ProductName.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || p.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || p.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return await MyHttpHelper.Json(req, HttpStatusCode.OK, allProducts);
    }

}