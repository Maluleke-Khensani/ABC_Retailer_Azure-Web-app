using System.Security.Claims;
using ABC_Retailers.Azure_Services;
using ABC_Retailers.Data;
using ABC_Retailers.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.EntityFrameworkCore;

namespace ABC_Retailers.Controllers
{
    public class LoginController : Controller
    {
        private readonly RetailersDbContext _db;
        private readonly ILogger<LoginController> _logger;

        public LoginController(RetailersDbContext db, ILogger<LoginController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Index(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login failed: ModelState invalid. Errors: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return View(model);
            }

            try
            {
                var loginResult = await AuthenticateUserAsync(model);
                if (loginResult == null)
                {
                    _logger.LogInformation("Login failed for username: {Username}", model.Username);
                    ViewBag.Error = "Invalid username or password.";
                    return View(model);
                }

                // store session
                HttpContext.Session.SetString("Username", loginResult.Username);
                HttpContext.Session.SetString("Role", loginResult.Role);
                HttpContext.Session.SetString("UserId", loginResult.UserId.ToString());

                if (string.Equals(loginResult.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("AdminDashboard", "Home");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                // After successful login
                if (loginResult.Role?.Trim() == "Admin")
                    return RedirectToAction("AdminDashboard", "Home");
                else if (loginResult.Role?.Trim() == "Customer")
                    return RedirectToAction("CustomerDashboard", "Home");
                else
                    return RedirectToAction("Index", "Home");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception during login for {Username}", model.Username);
                ViewBag.Error = "An error occurred during login. Please try again.";
                return View(model);
            }
        }

        private async Task<LoginResult?> AuthenticateUserAsync(LoginViewModel model)
        {
            var username = (model.Username ?? string.Empty).Trim();
            var password = (model.Password ?? string.Empty).Trim();

            _logger.LogInformation("Authenticating user: {Username}", username);

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user == null)
            {
                _logger.LogWarning("User not found: {Username}", username);
                return null;
            }

            if (!string.Equals(user.Password?.Trim(), password, StringComparison.Ordinal))
            {
                _logger.LogWarning("Incorrect password for {Username}", username);
                return null;
            }

            // ✅ Role comes from the database, not the model
            var role = (user.Role ?? "").Trim();

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, role),
        new Claim("UserId", user.Id.ToString())
    };

            const string authScheme = "CookieAuth";
            var identity = new ClaimsIdentity(claims, authScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(authScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
            });

            _logger.LogInformation("User {Username} logged in successfully as {Role}", username, role);

            return new LoginResult
            {
                Username = user.Username,
                Role = role,
                UserId = user.Id
            };
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            const string authScheme = "CookieAuth";
            await HttpContext.SignOutAsync(authScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }

    public class LoginResult
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}
