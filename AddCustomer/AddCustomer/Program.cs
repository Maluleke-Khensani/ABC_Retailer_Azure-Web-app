using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Create the builder
var builder = FunctionsApplication.CreateBuilder(args);

// Register TableServiceClient so DI can inject it
builder.Services.AddSingleton(x =>
{
    string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
    return new TableServiceClient(connectionString);
});

// Optional: Application Insights setup (keep this if you already had it)
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Build and run
builder.Build().Run();
