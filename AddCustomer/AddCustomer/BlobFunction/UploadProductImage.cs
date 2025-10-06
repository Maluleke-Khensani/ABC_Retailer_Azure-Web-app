using System.IO;
using System.Net;
using System.Threading.Tasks;
using ABC_Retails_Functions.HelperClasses;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ABC_Retails_Functions.BlobFunction;

public class UploadProductImage

{

    // Use STORAGE_CONNECTION (preferred) or fallback to AzureWebJobsStorage
    private readonly string _connection =  Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
    // container name can be overridden in local.settings.json with BLOB_PRODUCT_IMAGES
    private readonly string _container = Environment.GetEnvironmentVariable("BLOB_PRODUCT_IMAGES") ?? "product-images";



    private readonly ILogger<UploadProductImage> _logger;

    public UploadProductImage(ILogger<UploadProductImage> logger)
    {
        _logger = logger;
    }

    [Function("UploadProductImage")]
    public async Task<HttpResponseData> Run(
               [HttpTrigger(AuthorizationLevel.Function, "post", Route = "upload-product-image")] HttpRequestData req,
               FunctionContext ctx)
    {
        var log = ctx.GetLogger("UploadProductImage");
        log.LogInformation("UploadProductImage function started.");

        // Inspect Content-Type
        var contentType = req.Headers.TryGetValues("Content-Type", out var ctVals) ? ctVals.FirstOrDefault() ?? "" : "";

        if (!contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            log.LogWarning("Request is not multipart/form-data. Returning 400.");
            return await MyHttpHelper.Text(req, HttpStatusCode.BadRequest, "Content-Type must be multipart/form-data and include a file (field name: imageFile).");
        }

        // Parse multipart/form-data into text fields and files
        var form = await MultipartHelper.ParseAsync(req.Body, contentType);

        // Try a few common field names
        var file = form.Files.FirstOrDefault(f =>
            string.Equals(f.FieldName, "imageFile", StringComparison.OrdinalIgnoreCase)
            || string.Equals(f.FieldName, "file", StringComparison.OrdinalIgnoreCase)
            || string.Equals(f.FieldName, "ImageFile", StringComparison.OrdinalIgnoreCase));

        if (file == null || file.Data.Length == 0)
        {
            log.LogWarning("No file part named 'imageFile' found.");
            return await MyHttpHelper.Text(req, HttpStatusCode.BadRequest, "No file uploaded (expect field name 'imageFile').");
        }

        // Create container and upload blob
        var containerClient = new BlobContainerClient(_connection, _container);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var blobName = $"{Guid.NewGuid():N}-{file.FileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        file.Data.Position = 0;
        await blobClient.UploadAsync(file.Data, overwrite: false);

        var result = new { ImageUrl = blobClient.Uri.ToString() };

        log.LogInformation("Image uploaded to blob: {0}", blobClient.Uri);

        return await MyHttpHelper.Json(req, HttpStatusCode.Created, result);
    }
}
