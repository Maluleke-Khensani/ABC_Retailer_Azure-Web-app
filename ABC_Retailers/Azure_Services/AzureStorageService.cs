using System.Text.Json;
using ABC_Retailers.Data;
using ABC_Retailers.Models;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using Microsoft.EntityFrameworkCore;


namespace ABC_Retailers.Azure_Services
{
    //https://abcretailsfunctions-exdcghdvejcjgxc2.canadacentral-01.azurewebsites.net/api
    public class AzureStorageService : IAzureStorageService
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly QueueServiceClient _queueServiceClient;
        private readonly ShareServiceClient _shareServiceClient;
        private readonly ILogger<AzureStorageService> _logger;

        private readonly RetailersDbContext _dbContext;

        public AzureStorageService(
            IConfiguration configuration,
            ILogger<AzureStorageService> logger,
            RetailersDbContext dbContext) // ✅ add this
        {
            _dbContext = dbContext;
            string connectionString = configuration.GetConnectionString("AzureStorage")
                ?? throw new InvalidOperationException("Azure Storage connection string not found");

            _tableServiceClient = new TableServiceClient(connectionString);
            _blobServiceClient = new BlobServiceClient(connectionString);
            _queueServiceClient = new QueueServiceClient(connectionString);
            _shareServiceClient = new ShareServiceClient(connectionString);
            _logger = logger;

            InitializeStorageAsync().Wait();
        }


        public async Task InitializeStorageAsync()
        {
            try
            {
                _logger.LogInformation("Starting Azure Storage initialization...");

                // Create tables
                await _tableServiceClient.CreateTableIfNotExistsAsync("CustomerDetails");
                await _tableServiceClient.CreateTableIfNotExistsAsync("ProductDetails");
                await _tableServiceClient.CreateTableIfNotExistsAsync("Orders");
                await _tableServiceClient.CreateTableIfNotExistsAsync("Notifications");
                await _tableServiceClient.CreateTableIfNotExistsAsync("ProductCatalog");


                _logger.LogInformation("Tables created successfully");

                // Create blob containers with retry logic
                var productImagesContainer = _blobServiceClient.GetBlobContainerClient("product-images");
                await productImagesContainer.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

                var paymentProofsContainer = _blobServiceClient.GetBlobContainerClient("payment-proofs");
                await paymentProofsContainer.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.None);

                _logger.LogInformation("Blob containers created successfully");

                // Create queues
                var orderQueue = _queueServiceClient.GetQueueClient("order-notifications");
                await orderQueue.CreateIfNotExistsAsync();

                var stockQueue = _queueServiceClient.GetQueueClient("stock-updates");
                await stockQueue.CreateIfNotExistsAsync();

                _logger.LogInformation("Queues created successfully");

                // Create file share
                var contractsShare = _shareServiceClient.GetShareClient("contracts");
                await contractsShare.CreateIfNotExistsAsync();

                // Create payments directory in contracts share
                var contractsDirectory = contractsShare.GetDirectoryClient("payments");
                await contractsDirectory.CreateIfNotExistsAsync();

                _logger.LogInformation("File shares created successfully");

                _logger.LogInformation("Azure Storage initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Storage: {Message}", ex.Message);
                throw; // Re-throw to make the error visible
            }
        }

        // Table Operations
        public async Task<Products?> GetProductByIdAsync(string productId)
        {
            try
            {
                // Assuming Products table uses a fixed PartitionKey, e.g. "Products"
                string partitionKey = "Products";

                var product = await GetEntityAsync<Products>(partitionKey, productId);
                return product;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching product by ID: {ex.Message}");
                return null;
            }
        }


        public async Task<List<T>> GetAllEntitiesAsync<T>() where T : class, ITableEntity, new()
        {
            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);
            var entities = new List<T>();

            await foreach (var entity in tableClient.QueryAsync<T>())
            {
                entities.Add(entity);
            }

            return entities;
        }

        public async Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);

            try
            {
                var response = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                return response.Value;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task<T> AddEntityAsync<T>(T entity) where T : class, ITableEntity
        {
            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);

            await tableClient.AddEntityAsync(entity);
            return entity;
        }

        public async Task<T> UpdateEntityAsync<T>(T entity) where T : class, ITableEntity
        {
            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);

            try
            {
                // Use IfMatch condition for optimistic concurrency
                await tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
                return entity;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 412)
            {
                // Precondition failed - entity was modified by another process
                _logger.LogWarning("Entity update failed due to ETag mismatch for {EntityType} with RowKey {RowKey}",
                    typeof(T).Name, entity.RowKey);
                throw new InvalidOperationException("The entity was modified by another process. Please refresh and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating entity {EntityType} with RowKey {RowKey}: {Message}",
                    typeof(T).Name, entity.RowKey, ex.Message);
                throw;
            }
        }

        public async Task DeleteEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            var tableName = GetTableName<T>();
            var tableClient = _tableServiceClient.GetTableClient(tableName);

            await tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // Blob Operations
        public async Task<string> UploadImageAsync(IFormFile file, string containerName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

                // Ensure container exists
                await containerClient.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var blobClient = containerClient.GetBlobClient(fileName);

                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, overwrite: true);

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to container {ContainerName}: {Message}", containerName, ex.Message);
                throw;
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file, string containerName)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

                // Ensure container exists
                await containerClient.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.None);

                var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{file.FileName}";
                var blobClient = containerClient.GetBlobClient(fileName);

                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, overwrite: true);

                return fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to container {ContainerName}: {Message}", containerName, ex.Message);
                throw;
            }
        }

        public async Task DeleteBlobAsync(string blobName, string containerName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync();
        }

        // Queue Operations
        public async Task SendMessageAsync(string queueName, string message)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            await queueClient.SendMessageAsync(message);
        }

        public async Task<string?> ReceiveMessageAsync(string queueName)
        {
            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            var response = await queueClient.ReceiveMessageAsync();

            if (response.Value != null)
            {
                await queueClient.DeleteMessageAsync(response.Value.MessageId, response.Value.PopReceipt);
                return response.Value.MessageText;
            }

            return null;
        }

        // File Share Operations
        public async Task<string> UploadToFileShareAsync(IFormFile file, string shareName, string directoryName = "")
        {
            var shareClient = _shareServiceClient.GetShareClient(shareName);
            var directoryClient = string.IsNullOrEmpty(directoryName)
                ? shareClient.GetRootDirectoryClient()
                : shareClient.GetDirectoryClient(directoryName);

            await directoryClient.CreateIfNotExistsAsync();

            var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{file.FileName}";
            var fileClient = directoryClient.GetFileClient(fileName);

            // Create the file with correct length
            using var stream = file.OpenReadStream();
            await fileClient.CreateAsync(stream.Length);

            // Reset stream position to 0 before uploading
            stream.Position = 0;

            await fileClient.UploadAsync(stream);

            return fileName;
        }


        public async Task<byte[]> DownloadFromFileShareAsync(string shareName, string fileName, string directoryName = "")
        {
            var shareClient = _shareServiceClient.GetShareClient(shareName);
            var directoryClient = string.IsNullOrEmpty(directoryName)
                ? shareClient.GetRootDirectoryClient()
                : shareClient.GetDirectoryClient(directoryName);

            var fileClient = directoryClient.GetFileClient(fileName);
            var response = await fileClient.DownloadAsync();

            using var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream);

            return memoryStream.ToArray();
        }

        // --- SQL CART OPERATIONS (using Entity Framework) ---

        public async Task AddToCartAsync(Cart cart)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            _dbContext.Cart.Add(cart);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"Product {cart.ProductId} added to cart for {cart.CustomerUsername}");
        }

        public async Task<IEnumerable<Cart>> GetCartItemsByUserAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            var items = await _dbContext.Cart
                .Where(c => c.CustomerUsername == username)
                .ToListAsync();

            return items;
        }

        public async Task PlaceOrderFromCartAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            var cartItems = await _dbContext.Cart
                .Where(c => c.CustomerUsername == username)
                .ToListAsync();

            if (!cartItems.Any())
                throw new InvalidOperationException("Cart is empty.");

            var catalogProducts = await GetAllEntitiesAsync<ProductCatalog>();

            // 🪄 create a separate Order entry per product
            foreach (var cartItem in cartItems)
            {
                var product = catalogProducts.FirstOrDefault(p => p.RowKey == cartItem.ProductId);
                if (product != null)
                {
                    var order = new Orders
                    {
                        PartitionKey = username,
                        RowKey = Guid.NewGuid().ToString(),
                        CustomerId = username,
                        Username = username,
                        ProductId = product.RowKey,
                        ProductName = product.ProductName,
                        Quantity = cartItem.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * cartItem.Quantity,
                        OrderDate = DateTime.UtcNow,
                        Status = "Placed"
                    };

                    await AddEntityAsync(order);
                }
            }

            // clear the SQL cart
            _dbContext.Cart.RemoveRange(cartItems);
            await _dbContext.SaveChangesAsync();

            // send queue notification for the new order batch
            var message = JsonSerializer.Serialize(new
            {
                PartitionKey = username,
                RowKey = Guid.NewGuid().ToString()
            });

            await SendMessageAsync("order-notifications", message);
            _logger.LogInformation($"✅ Orders placed successfully for {username} ({cartItems.Count} items).");
        }


        public async Task DeleteCartItem(int cartId)
        {
            var item = await _dbContext.Cart.FirstOrDefaultAsync(c => c.Id == cartId);
            if (item != null)
            {
                _dbContext.Cart.Remove(item);
                await _dbContext.SaveChangesAsync();
            }
        }

        



        private static string GetTableName<T>()
        {
            return typeof(T).Name switch
            {
                nameof(Customers) => "CustomerDetails",
                nameof(Products) => "ProductDetails",
                nameof(ProductCatalog) => "ProductCatalog",
                nameof(Orders) => "Orders",
                nameof(Notifications) => "Notifications",   

                _ => typeof(T).Name + "s"
            };
        }
    }
}
