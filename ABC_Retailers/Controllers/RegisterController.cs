using ABC_Retailers.Azure_Services;
using ABC_Retailers.Data;
using ABC_Retailers.Models;
using ABC_Retailers.Models.Login_Register;
using ABC_Retailers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABC_Retailers.Controllers
{
    public class RegisterController : Controller
    {
        private readonly RetailersDbContext _db;
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<LoginController> _logger;


        public RegisterController(RetailersDbContext db, IFunctionsApi functionsApi, ILogger<LoginController> logger)
        {
            _db = db;
            _functionsApi = functionsApi;
            _logger = logger;
        }

        
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new RegisterViewModel());
        }
       
        /*[HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }
*/
/*
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 1️⃣ Check duplicate username
            var exists = await _db.Users.AnyAsync(u => u.Username == model.Username);
            if (exists)
            {
                ViewBag.Error = "Username already exists.";
                return View(model);
            }

            try
            {
                // 2️⃣ Save local user (SQL)
                var user = new Users
                {
                    Username = model.Username,
                    Password = model.Password, // TODO: Replace with hashed password later
                    Role = model.Role
                };
                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                // 3️⃣ Save to Azure Function
                var customer = new Customers
                {
                    Username = model.Username,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    ShippingAddress = model.ShippingAddress
                };

                await _functionsApi.CreateCustomerAsync(customer);

                TempData["Success"] = "Registration successful! Please log in.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for user {Username}", model.Username);
                ViewBag.Error = "Could not complete registration. Please try again later.";
                return View(model);
            }
        }
        */
    
        // POST: /Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check if username already exists
            var exists = await _db.Users.AnyAsync(u => u.Username == model.Username);
            if (exists)
            {
                ViewBag.Error = "Username already exists.";
                return View(model);
            }

            // ✅ 1. Save to SQL: create User
            var user = new Users
            {
                Username = model.Username,
                Password = model.Password, // ⚠️ Plaintext — replace with hashed version in production
                Role = model.Role
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // ✅ 2. Save to Azure Table Storage via Azure Function
            var customer = new Customers
            {
                Username = model.Username,
               FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                ShippingAddress = model.ShippingAddress
            };
            await _functionsApi.CreateCustomerAsync(customer);

            TempData["Success"] = "Account created successfully. Please login.";
            return RedirectToAction("Index", "Login");
        }
    
    }
}
