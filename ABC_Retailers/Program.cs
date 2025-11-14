using ABC_Retailers.Azure_Services;
using ABC_Retailers.Data;
using ABC_Retailers.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add MVC Controllers and Views
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();


// Register Azure Storage Service (Singleton for global access)
builder.Services.AddScoped<IAzureStorageService, AzureStorageService>();

// Add HttpClient support
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IFunctionsApi, FunctionsApi>();

// Configure SQL Database (Azure SQL)
builder.Services.AddDbContext<RetailersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RetailersDatabse"))
);

// Configure Cookie Authentication
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "UserLoginCookie";
        options.LoginPath = "/Login/Index";
        options.AccessDeniedPath = "/Login/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
    });

// Configure Sessions
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "UserSessionCookie";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

var app = builder.Build();

// Configure Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Default MVC Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// ✅ Force initialization of AzureStorageService when the app starts
try
{
    using (var scope = app.Services.CreateScope())
    {
        var storageService = scope.ServiceProvider.GetRequiredService<IAzureStorageService>();
        Console.WriteLine("✅ Azure Storage Service initialized successfully!");
    }
}
catch (Exception ex)
{
    Console.WriteLine("❌ Failed to initialize Azure Storage Service: " + ex.Message);
}

app.Run();
