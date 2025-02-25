using Microsoft.AspNetCore.Mvc;
using devDynast.Data;
using devDynast.Models;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace devDynast.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
{
    if (ModelState.IsValid)
    {
        // Retrieve the user from the database
        var user = _context.Users.FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password); // Consider hashing the password for better security

        if (user != null)
        {
            int userId = user.Id ?? 0;
            string userRole = user.Role;

            // Create claims for the user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "User") // Default to "User" if role is null
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Sign in the user with claims
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            
            HttpContext.Session.SetInt32("UserId", userId);
           
            HttpContext.Session.SetString("Role", userRole);

            // Redirect based on the user's role
            if (user.Role == "Admin")
            {
                return RedirectToAction("Dashboard", "Admin"); // Redirect to Admin Dashboard
            }
            else if (user.Role == "User")
            {
                return RedirectToAction("Menu", "User"); // Redirect to   <a href="@Url.Action("Menu", "User")">
            }

            // Redirect to Home if no specific role was found (or for other users)
            return RedirectToAction("Index", "Home");
        }

        // If user was not found, add an error message
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
    }

    // If we get here, something failed, redisplay the form with errors
    return View(model);
}

       

        // GET: /Account/Logout
        public async Task<IActionResult> Logout()
        {
            // Sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear the session
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }

         public IActionResult Dashboard()
        {
            return View(); // Return the Dashboard view
        }
    }
}
