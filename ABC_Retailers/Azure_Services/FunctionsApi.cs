using System.Net.Http.Headers;
using System.Text.Json;
using ABC_Retailers.Azure_Services;
using ABC_Retailers.Controllers;
using ABC_Retailers.Models;

public class FunctionsApi : IFunctionsApi
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FunctionsApi> _logger;
    private readonly string _functionBaseUrl;
    private readonly string _sendToQueueKey;
    private readonly string _uploadImageKey;
    private readonly string _productsFunctionKey;
    private readonly string _uploadFileKey;


    public FunctionsApi(HttpClient httpClient, ILogger<FunctionsApi> logger, IConfiguration configuration)
    {
        _functionBaseUrl = configuration["FunctionSettings:BaseUrl"];
        _sendToQueueKey = configuration["FunctionSettings:SendToQueueKey"];
        _uploadImageKey = configuration["FunctionSettings:UploadImageKey"];
        _productsFunctionKey = configuration["FunctionSettings:ProductsFunctionKey"]; 
        _uploadFileKey = configuration["FunctionSettings:UploadFileKey"]; 



        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_functionBaseUrl);
        _logger = logger;
    }

    public async Task CreateCustomerAsync(Customers customer)
    {
        if (customer == null)
            throw new ArgumentNullException(nameof(customer), "Customer cannot be null");

        string functionUrl = $"customers?code={_productsFunctionKey}";

        var json = JsonSerializer.Serialize(customer, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.LogInformation($"Sending JSON to function:\n{json}");

        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(functionUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new Exception($"Function returned {response.StatusCode}: {message}");
        }

        _logger.LogInformation("Customer created successfully via function.");
    }

    public async Task<Customers> GetCustomerByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or empty", nameof(username));

        string url = $"customers/{Uri.EscapeDataString(username)}";
        _logger.LogInformation($"Fetching customer by username: {username}");

        var response = await _httpClient.GetAsync(url);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning($"Customer '{username}' not found.");
            return null!;
        }

        response.EnsureSuccessStatusCode();
        var customer = await response.Content.ReadFromJsonAsync<Customers>();

        if (customer == null)
            _logger.LogWarning($"Customer data for '{username}' was empty.");

        return customer!;
    }

    public async Task GetCustomerAsync(string customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("CustomerId cannot be null or empty", nameof(customerId));

        var url = $"customers/{Uri.EscapeDataString(customerId)}";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to fetch customer: {response.StatusCode} - {error}");
        }

    }




    public async Task SendProductUpdateAsync(ProductCatalog product)
    {
        string functionUrl = $"send-to-queue?code={_sendToQueueKey}";

        var notificationMessage = new
        {
            Message = $"Stock updated for {product.ProductName}",
            Category = product.Category ?? "",
            ProductName = product.ProductName,
            NewStock = product.Stock
        };

        var json = JsonSerializer.Serialize(notificationMessage);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(functionUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new Exception($"Function error: {message}");
        }
    }


    public async Task<string> UploadProductImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is null or empty");

        string functionUrl = $"upload-product-image?code={_uploadImageKey}";

        using var content = new MultipartFormDataContent();
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;

        var fileContent = new StreamContent(ms);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");

        content.Add(fileContent, "imageFile", file.FileName);

        var response = await _httpClient.PostAsync(functionUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new Exception($"Function error: {message}");
        }

        var dto = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>() ?? new();
        if (dto.TryGetValue("ImageUrl", out var url)) return url;

        throw new Exception("Function did not return ImageUrl");
    }


    public async Task CreateProductAsync(Products product)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product), "Product cannot be null");

        if (string.IsNullOrWhiteSpace(product.ProductName))
            throw new ArgumentException("ProductName cannot be empty");

        if (string.IsNullOrWhiteSpace(product.Category))
            throw new ArgumentException("Category cannot be empty");

        var payload = new
        {
            product.ProductName,
            product.Category,
            product.Stock,
            product.Price
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.LogInformation($"Sending JSON to function:\n{json}");

        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("products", content);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new Exception($"Function error: {message}");
        }

        _logger.LogInformation("Product created successfully via function.");
    }


    public async Task CreateOrderAsync(Orders order)
    {
        if (order == null)
            throw new ArgumentNullException(nameof(order), "Order cannot be null");

        string functionUrl = "orders";

        var json = JsonSerializer.Serialize(order);
        Console.WriteLine($"Sending JSON to function:\n{json}");

        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(functionUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new Exception($"Function error: {message}");
        }

        Console.WriteLine("Order sent successfully.");
    }


    public async Task<string> UploadFileToShareAsync(IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        content.Add(fileContent, "file", file.FileName);

        var functionUrl = $"files/upload?code={_uploadFileKey}";
        var response = await _httpClient.PostAsync(functionUrl, content);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsStringAsync();

        var error = await response.Content.ReadAsStringAsync();
        return $"Error: {response.StatusCode} - {error}";
    }



    public async Task<Orders> GetOrderByCustomerIdAsync(string customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("CustomerId cannot be null or empty", nameof(customerId));

        string url = $"orders/{Uri.EscapeDataString(customerId)}";
        _logger.LogInformation($"Fetching order for customer ID: {customerId}");

        var response = await _httpClient.GetAsync(url);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning($"No order found for customer ID: {customerId}");
            return null!;
        }

        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<Orders>();
        return order!;
    }
}
