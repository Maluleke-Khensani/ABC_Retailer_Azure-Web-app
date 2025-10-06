using System.Net;
using ABC_Retails_Functions.HelperClasses;
using Azure;
using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ABC_Retails_Functions;

public class FileShareUploadFunction
{
    private readonly ILogger<FileShareUploadFunction> _logger;
    private readonly string _conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
    private readonly string _shareName = "contracts"; // Your Azure File Share name

    public FileShareUploadFunction(ILogger<FileShareUploadFunction> logger)
    {
        _logger = logger;
    }
    [Function("UploadFileToShare")]
    public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "files/upload")] HttpRequestData req)
    {
        try
        {
            // Convert Function request to HttpContext style for form parsing
            var context = new DefaultHttpContext();
            context.Request.Body = req.Body;
            context.Request.ContentType = req.Headers.GetValues("Content-Type").FirstOrDefault();

            var form = await context.Request.ReadFormAsync();
            var file = form.Files.FirstOrDefault();

            if (file == null)
            {
                return await MyHttpHelper.Text(req, HttpStatusCode.BadRequest, "No file uploaded.");
            }

            // Connect to Azure File Share
            var shareClient = new ShareClient(_conn, _shareName);
            await shareClient.CreateIfNotExistsAsync();

            var directory = shareClient.GetRootDirectoryClient();
            var fileClient = directory.GetFileClient(file.FileName);

            using (var stream = file.OpenReadStream())
            {
                await fileClient.CreateAsync(stream.Length);
                await fileClient.UploadRangeAsync(new HttpRange(0, stream.Length), stream);
            }

            _logger.LogInformation($"File '{file.FileName}' uploaded successfully to Azure File Share.");
            return await MyHttpHelper.Text(req, HttpStatusCode.OK, $"File '{file.FileName}' uploaded successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error uploading file: {ex.Message}");
            return await MyHttpHelper.Text(req, HttpStatusCode.InternalServerError, $"Error: {ex.Message}");
        }
    }
}