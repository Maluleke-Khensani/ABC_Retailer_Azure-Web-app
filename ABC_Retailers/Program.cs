using ABC_Retailers.Azure_Services;
using ABC_Retailers.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Register Azure Storage service
builder.Services.AddSingleton<IAzureStorageService, AzureStorageService>();

// Add default HttpClient
builder.Services.AddHttpClient();

// Register FunctionsApi with HttpClient, ILogger, and IConfiguration injected
builder.Services.AddHttpClient<IFunctionsApi, FunctionsApi>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

// Map static assets (if this is your extension method)
app.MapStaticAssets();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
