using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using devDynast.Data;
using devDynast.Models;
using System.Linq;
using System.Threading.Tasks;

namespace devDynast.Controllers
{
    [Authorize]
    public class OrderHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderHistoryController> _logger;

        public OrderHistoryController(ApplicationDbContext context, ILogger<OrderHistoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> OrderHistory()
        {
            // Retrieve the user ID from the session
            var userIdInt = HttpContext.Session.GetInt32("UserId");

        
            if (userIdInt.HasValue)
            {
                var userId = userIdInt.Value.ToString();

                // Fetch past orders
                var pastOrders = _context.Cart
                    .Where(c => c.UserId == userId && c.Status == "paid")
                    .ToList();

                // Calculate total price
                var totalPrice = pastOrders.Sum(c => c.Price * c.Quantity);

                // Pass orders and total price to the view
                ViewBag.TotalPrice = totalPrice;
                return View(pastOrders);
            }
            else
            {
                
                return RedirectToAction("Login", "Account");
            }
        }
    }
}
