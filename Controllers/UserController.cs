using Microsoft.AspNetCore.Mvc;
using devDynast.Data;
using devDynast.Models;
using Microsoft.Extensions.Logging; 
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using devDynast.ViewModels;

namespace devDynast.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(ApplicationDbContext context, ILogger<UserController> logger) 
        {
            _context = context;
            _logger = logger; 
        }
        // GET: /User/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /User/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            if (ModelState.IsValid)
            {
                // Add user to database
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Home");
            }
            return View(user);
        }

        // GET: User/Dashboard
  public IActionResult Dashboard()
{
    var userIdInt = HttpContext.Session.GetInt32("UserId");

    // Check if the userIdInt is null (user is not logged in)
    if (!userIdInt.HasValue)
    {
        _logger.LogWarning("UserId not found in session.");
        return RedirectToAction("Login", "Account");
    }

    // Fetch unread notifications
    var unreadNotifications = _context.Notifications
        .Where(n => n.UserId == userIdInt.Value && !n.IsRead && n.Intended == "customer")
        .ToList();

    // Set the unread notifications count to ViewBag
    ViewBag.UnreadNotificationsCount = unreadNotifications.Count;

    // Create a list of notification view models
    var notifications = unreadNotifications.Select(n => new NotificationViewModel
    {
        Message = n.Message,
        CreatedAt = n.CreatedAt
    }).ToList();

    return View(notifications);
}


    public IActionResult Menu(string category)
    {
        // Fetch all menus
    var menus = _context.Menus.ToList();

    // Fetch MenuItems based on the selected category, if provided
    var menuItems = string.IsNullOrEmpty(category) 
        ? _context.MenuItems.ToList() // If no category is selected, fetch all items
        : _context.MenuItems.Where(m => m.Category == category).ToList();

    var viewModel = new MenuViewModel
    {
        Menus = menus,
        MenuItems = menuItems // Pass the filtered menu items based on the category
    };

   return View("~/Views/User/Menu.cshtml", viewModel);
    }

    // GET: User/EditProfile
public async Task<IActionResult> EditProfile()
{
    var userId = HttpContext.Session.GetInt32("UserId");

    if (userId == null)
    {
        return NotFound();
    }

    var user = await _context.Users.FindAsync(userId.Value);
    if (user == null)
    {
        return NotFound();
    }

    return View(user);
}

 // POST: User/EditProfile
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditProfile([Bind("Id,FirstName,LastName,Email,PhoneNumber,Role")] User user)
{
    _logger.LogInformation("EditProfile POST action called.");
    _logger.LogInformation("User profile requested for UserId: {UserId}", user.Id);

    var userId = HttpContext.Session.GetInt32("UserId");
    if (userId == null)
    {
        _logger.LogWarning("UserId is null in session.");
        return NotFound();
    }

    user.Id = userId.Value;

    if (ModelState.IsValid)
    {
        try
        {
            var existingUser = await _context.Users.FindAsync(userId);
            if (existingUser != null)
            {
                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.PhoneNumber = user.PhoneNumber;
               

                _context.Update(existingUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User profile updated successfully for UserId: {UserId}", user.Id);
                return RedirectToAction("Dashboard", "User"); 
            }
            else
            {
                _logger.LogWarning("User not found for UserId: {UserId}", user.Id);
                return NotFound();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile for UserId: {UserId}", user.Id);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the profile. Please try again.");
        }
    }
    else
    {
        _logger.LogWarning("ModelState is invalid for UserId: {UserId}", user.Id);
        foreach (var modelStateKey in ModelState.Keys)
        {
            var modelStateVal = ModelState[modelStateKey];
            foreach (var error in modelStateVal.Errors)
            {
                _logger.LogError("ModelState Error: Key = {Key}, Error = {Error}", modelStateKey, error.ErrorMessage);
            }
        }
    }

    return View(user); // Return the view with validation errors
}

// GET: Feedback/Create
        

        // GET: User/Index
        public IActionResult Index()
        {
            return View();
        }

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Feedback(Feedback feedback)
{
    var userId = HttpContext.Session.GetInt32("UserId");

    if (userId == null)
    {
        _logger.LogWarning("User not found in session.");
        return RedirectToAction("Error", "Home");
    }

    feedback.UserId = userId.Value.ToString();

    // Check the feedback order number
    if (string.IsNullOrEmpty(feedback.OrderNumber))
    {
        _logger.LogWarning("OrderNumber is null or empty.");
    }

    if (ModelState.IsValid)
    {
        // Save feedback to the database
        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();
        return RedirectToAction("GetOrderDetails", "User");
    }

    var errors = ModelState.Values.SelectMany(v => v.Errors);
    foreach (var error in errors)
    {
        _logger.LogWarning("Model error: {Error}", error.ErrorMessage);
    }

    return View(feedback);
}


public IActionResult GetOrderDetails()
{
    var userId = HttpContext.Session.GetInt32("UserId");
    if (userId == null)
    {
        return RedirectToAction("Login", "User");
    }

    // Fetch all orders that haven't been complained about
    var groupedOrders = _context.Cart
        .Where(c => c.Status == "Order Collected" && c.UserId == userId.ToString())
        .Join(_context.MenuItems,
              cart => cart.ProductId,
              menuItem => menuItem.Id.ToString(),
              (cart, menuItem) => new
              {
                  cart.OrderNumber,
                  cart.Quantity,
                  cart.Price,
                  cart.Status,
                  cart.CreatedAt,
                  MenuItemName = menuItem.Name,
                  ExtraIds = cart.ExtraIds
              })
        .GroupJoin(_context.Feedbacks, 
                   order => order.OrderNumber,
                   complaint => complaint.OrderNumber,
                   (order, complaints) => new { order, complaints })
        .Where(x => !x.complaints.Any()) // Exclude orders with complaints
        .GroupBy(x => x.order.OrderNumber)
        .Select(g => new

        {
            OrderNumber = g.Key,
            TotalQuantity = g.Sum(x => x.order.Quantity),
            TotalPrice = g.Sum(x => x.order.Price * x.order.Quantity),
            Status = g.First().order.Status,
            CreatedAt = g.First().order.CreatedAt,
            OrderItems = g.Select(o => new
            {
                ProductName = o.order.MenuItemName,
                Quantity = o.order.Quantity,
                ExtraIds = o.order.ExtraIds
            }).ToList()
        })
        .ToList();

    // Map the result to your view models
    var orderViewModels = groupedOrders.Select(order => new OrderSummaryViewModel
    {
        OrderNumber = order.OrderNumber,
        TotalQuantity = order.TotalQuantity,
        TotalPrice = order.TotalPrice,
        Status = order.Status,
        CreatedAt = order.CreatedAt,
        OrderItems = order.OrderItems.Select(item => new OrderDetailsViewModel
        {
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            Extras = item.ExtraIds != null
                ? item.ExtraIds.Split(',')
                    .Select(extraId => _context.MenuIngredient.FirstOrDefault(e => e.Id.ToString() == extraId)) // Fetch extras from DB
                    .Where(e => e != null)
                    .Select(e => new ExtraViewModel
                    {
                        Name = e.Name,
                        Price = e.Price
                    })
                    .ToList()
                : new List<ExtraViewModel>()
        }).ToList()
    }).ToList();

    return View("~/Views/User/Feedback.cshtml", orderViewModels);
}



[HttpGet]
public IActionResult Feedback(string OrderNumber)
{
    var model = new FeedbackViewModel
    {
        Feedback = new Feedback
        {
            OrderNumber = OrderNumber 
        },
        Orders = GetOrders(), // Fetching all orders
        OrderItems = GetOrderItems(OrderNumber) 
    };

    return View("~/Views/User/FeedbackOrders.cshtml", model);
} 

private IEnumerable<OrderDetailsViewModel> GetOrderItems(string orderNumber)
{
    // Fetch cart items and related menu items from the database
    var orderItems = _context.Cart
        .Where(c => c.OrderNumber == orderNumber)
        .Join(_context.MenuItems,
              cart => cart.ProductId,
              menuItem => menuItem.Id.ToString(),
              (cart, menuItem) => new OrderDetailsViewModel
              {
                  Id = cart.Id,
                  ProductName = menuItem.Name,
                  Price = cart.Price,
                  Quantity = cart.Quantity,
                  Date = cart.CreatedAt,
                  ProductImage = menuItem.ImageUrl,
                  Category = menuItem.Category,
                  Status = cart.Status,
                  OrderNumber = cart.OrderNumber,

                 
                  ExtraIds = cart.ExtraIds
              })
        .ToList();

  
    // Now handle the extras, mapping from ExtraIds to MenuIngredients
    foreach (var item in orderItems)
    {
        if (!string.IsNullOrEmpty(item.ExtraIds))
        {
            var extraIds = item.ExtraIds.Split(','); // Split the ExtraIds by commas
            item.Extras = _context.MenuIngredient
                .Where(ingredient => extraIds.Contains(ingredient.Id.ToString()))
                .Select(ingredient => new ExtraViewModel
                {
                    Name = ingredient.Name,
                    Price = ingredient.Price
                })
                .ToList();  // Fetch the extras and assign to the item
        }
        else
        {
            item.Extras = new List<ExtraViewModel>();  
        }
    }

    return orderItems;
}



private IEnumerable<OrderSummaryViewModel> GetOrders()
{
    var userId = HttpContext.Session.GetInt32("UserId");

    
    var groupedOrders = _context.Cart
    .Where(c => c.Status == "Order Collected")
    .Join(_context.MenuItems,
          cart => cart.ProductId,
          menuItem => menuItem.Id.ToString(),
          (cart, menuItem) => new
          {
              cart.OrderNumber,
              cart.Quantity,
              cart.Price,
              cart.Status,
              cart.CreatedAt
          })
    .GroupBy(order => order.OrderNumber)
    .Select(g => new OrderSummaryViewModel
    {
        OrderNumber = g.Key,
        TotalQuantity = g.Sum(x => x.Quantity),
        TotalPrice = g.Sum(x => x.Price * x.Quantity),
        Status = g.First().Status,
        CreatedAt = g.First().CreatedAt,
        
        
        HasComplained = _context.Feedbacks
            .Where(f => f.OrderNumber == g.Key && f.UserId == userId.ToString())
            .Any() // Check if a complaint exists for the current order and user
    })
    .ToList();
    foreach (var order in groupedOrders)
{
    Console.WriteLine($"Order {order.OrderNumber}, HasComplained: {order.HasComplained}");
}


    return groupedOrders;
}


public IActionResult MyFeedback()
{
    var userId = HttpContext.Session.GetString("UserId"); // Retrieve logged-in user's ID

    try
    {
        var feedbacks = _context.Feedbacks
            .Where(f => f.UserId == userId)
            .Select(f => new UserFeedbackViewModel
            {
                FeedbackId = f.Id,
                UserId = f.UserId,
                Title = f.Title,
                Content = "Just checking the content",
                OrderNumber = f.OrderNumber,
                CreatedAt = f.CreatedAt,
                IsAdminResponded = f.IsAdminResponded,
                AdminResponse = f.AdminResponse ?? "No response yet." 
            }).ToList();

        return View(feedbacks);
    }
    catch (Exception ex)
    {
        // Log the exception 
         Console.WriteLine(ex.ToString());
        return View("Error"); 
    } 
}

 public IActionResult OrderSummary()
{
    var orders = _context.Cart
        .Where(c => c.Status == "Order Collected") // Filter for "Order Collected"
        .GroupBy(c => c.OrderNumber)
        .Select(g => new OrderSummaryViewModel
        {
            OrderNumber = g.Key,
            TotalQuantity = g.Sum(c => c.Quantity),
            TotalPrice = g.Sum(c => c.Price * c.Quantity),
            Status = g.FirstOrDefault().Status,
            CreatedAt = g.Min(c => c.CreatedAt),
            HasRated = _context.Ratings.Any(r => r.OrderNumber == g.Key)
        })
        .ToList();

    return View(orders);
}


 public IActionResult RateOrder(string OrderNumber)
{
    // Materialize the cart and menu item data first
    var cartMenuItems = _context.Cart
        .Where(c => c.OrderNumber == OrderNumber)
        .Join(_context.MenuItems,
              cart => cart.ProductId,
              menuItem => menuItem.Id.ToString(),
              (cart, menuItem) => new 
              {
                  Cart = cart,
                  MenuItem = menuItem
              })
        .ToList(); // Materialize the query into memory

    if (cartMenuItems == null || !cartMenuItems.Any())
    {
        return NotFound("No order details found for this order.");
    }

    var orderDetails = cartMenuItems.Select(cartMenuItem => new OrderDetailsViewModel
    {
        Id = cartMenuItem.Cart.Id,
        ProductName = cartMenuItem.MenuItem.Name,
        Price = cartMenuItem.Cart.Price,
        Quantity = cartMenuItem.Cart.Quantity,
        Date = cartMenuItem.Cart.CreatedAt,
        ProductImage = cartMenuItem.MenuItem.ImageUrl,
        Category = cartMenuItem.MenuItem.Category,
        Status = cartMenuItem.Cart.Status,
        OrderNumber = cartMenuItem.Cart.OrderNumber,

        Extras = cartMenuItem.Cart.ExtraIds != null 
            ? _context.MenuIngredient
                .Where(e => cartMenuItem.Cart.ExtraIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Contains(e.Id.ToString()))
                .Select(e => new ExtraViewModel
                {
                    Name = e.Name,
                    Price = e.Price
                }).ToList()
            : new List<ExtraViewModel>()
    }).ToList();

    var model = new RateOrderViewModel
    {
        OrderNumber = OrderNumber,
        OrderDetails = orderDetails
    };

    return View(model);
}



[HttpPost]
public IActionResult SubmitRating(string orderNumber, int ratingValue, string comment)
{
    // Retrieve the user ID from the session as int
            var userIdInt = HttpContext.Session.GetInt32("UserId");

            // Convert to string
            var userId = userIdInt?.ToString();
    
    // Log the userId for debugging purposes
    if (string.IsNullOrEmpty(userId))
    {
        _logger.LogWarning("UserId is null or empty.");
    }
    else if (userId.Contains("\0"))
    {
        _logger.LogWarning("UserId contains a null byte.");
    }
    // Validate the inputs
    if (!string.IsNullOrEmpty(orderNumber) && ratingValue > 0 && !string.IsNullOrEmpty(comment))
    {
        var rating = new Rating
        {
            OrderNumber = orderNumber,
            RatingValue = ratingValue,
            Comment = comment,
            UserId = userId, 
            CreatedAt = DateTime.UtcNow
        };

        _context.Ratings.Add(rating);
        _context.SaveChanges();

        return RedirectToAction("OrderSummary");
    }
    else
    {
        _logger.LogInformation("Please provide a valid rating and comment.");
    }

    // Repopulate the order details in case of validation failure
    var orderDetails = _context.Cart
        .Where(c => c.OrderNumber == orderNumber)
        .Join(_context.MenuItems,
              cart => cart.ProductId,
              menuItem => menuItem.Id.ToString(),
              (cart, menuItem) => new OrderDetailsViewModel
              {
                  Id = cart.Id,
                  ProductName = menuItem.Name,
                  Price = cart.Price,
                  Quantity = cart.Quantity,
                  Date = cart.CreatedAt,
                  ProductImage = menuItem.ImageUrl,
                  Category = menuItem.Category,
                  Status = cart.Status,
                  OrderNumber = cart.OrderNumber
              })
        .ToList();

        // Check if the user has already rated this order
    var hasRated = _context.Ratings.Any(r => r.OrderNumber == orderNumber && r.UserId == userId);

    var model = new RateOrderViewModel
    {
        OrderNumber = orderNumber,
        OrderDetails = orderDetails,
        HasRated = hasRated
    };

    return View("RateOrder", model);
}

        
// GET: Ratings
        public IActionResult Items()
{
    // Retrieve the user ID from the session as int
    var userIdInt = HttpContext.Session.GetInt32("UserId");
    
    // Convert to string
    var userId = userIdInt?.ToString();

    
    _logger.LogInformation("Retrieved UserId from session: {UserId}", userId);

    // Check if the userId is not null
    if (!string.IsNullOrEmpty(userId))
    {
        var cartItems = _context.Cart
            .Where(c => c.UserId == userId && c.Status == "paid")
            .Join(_context.MenuItems,
                  cart => cart.ProductId,
                  menuItem => menuItem.Id.ToString(),
                  (cart, menuItem) => new
                  {
                      cart.ProductId,
                      cart.Quantity,
                      cart.Price,
                      cart.Status,
                      cart.CreatedAt,
                      ProductName = menuItem.Name 
                  })
            .ToList();

        return View(cartItems);
    }
    else
    {
        return RedirectToAction("Login", "Account");
    }
}

public async Task<IActionResult> OrderHistory()
{
    
    var userIdInt = HttpContext.Session.GetInt32("UserId");

    _logger.LogInformation("Retrieved UserId from session: {UserId}", userIdInt);

    if (userIdInt.HasValue)
    {
        var userId = userIdInt.Value.ToString();

        // Fetch past orders grouped by order number and sum total price for each order number
        var orderGroups = _context.Cart
            .Where(c => c.UserId == userId && c.Status == "paid")
            .GroupBy(c => c.OrderNumber)
            .Select(g => new
            {
                OrderNumber = g.Key,
                TotalPrice = g.Sum(c => c.Price * c.Quantity)
            })
            .ToList();

        return View(orderGroups);  
    }
    else
    {
        return RedirectToAction("Login", "Account");
    }
}


public async Task<IActionResult> OrderDetails(string orderNumber)
{
    var userIdInt = HttpContext.Session.GetInt32("UserId");

    if (userIdInt.HasValue)
    {
        var userId = userIdInt.Value.ToString();

        // Fetch items in the specific order by orderNumber
        var orderDetails = _context.Cart
            .Where(c => c.UserId == userId && c.OrderNumber == orderNumber && c.Status == "paid")
            .Join(
                _context.MenuItems,
                cart => cart.ProductId,
                menu => menu.Id.ToString(),
                (cart, menu) => new
                {
                    cart.ProductId,
                    cart.Quantity,
                    cart.Price,
                    cart.CreatedAt,
                    ProductName = menu.Name
                })
            .ToList();

        return View(orderDetails);
    }
    else
    {
        return RedirectToAction("Login", "Account");
    }
}

        [HttpPost]
    public ActionResult addToCart(String productId, decimal price)
    {
        try
        {
            // Retrieve the user ID from the session as int
            var userIdInt = HttpContext.Session.GetInt32("UserId");

            // Convert to string
            var userId = userIdInt?.ToString();

            var cartItem = new Cart
            {
                UserId = userId,
                ProductId = productId,
                Quantity = 1,  
                Price = (double)price,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Cart.Add(cartItem);
            _context.SaveChanges();

            return Json(new { success = true, message = "Item added to cart." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error adding item to cart: " + ex.Message });
        }
    }

   public IActionResult GetNotifications()
{
    var userIdInt = HttpContext.Session.GetInt32("UserId");

    // Check if the userIdInt is nu
    if (!userIdInt.HasValue)
    {
        _logger.LogWarning("UserId not found in session.");
        return RedirectToAction("Login", "Account");
    }


    var unreadNotifications = _context.Notifications
       .Where(n => n.UserId == userIdInt.Value && !n.IsRead && n.Intended == "customer")
       .ToList();


    ViewBag.UnreadNotificationsCount = unreadNotifications.Count;

    return View("NotificationBell", unreadNotifications);
}


    public IActionResult MarkAsRead()
{
    var userId = HttpContext.Session.GetString("UserId");

    if (!string.IsNullOrEmpty(userId))
    {
        var unreadNotifications = _context.Notifications
            .Where(n => n.UserId == int.Parse(userId) && !n.IsRead)
            .ToList();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        _context.SaveChanges();
    }

    return RedirectToAction("GetNotifications");
}


public IActionResult ClearNotifications()
{
    var userIdInt = HttpContext.Session.GetInt32("UserId");

    // Check if the userIdInt is null
    if (!userIdInt.HasValue)
    {
        _logger.LogWarning("UserId not found in session.");
        return RedirectToAction("Login", "Account");
    }

    // Get all unread notifications for the user and mark them as read
    var notifications = _context.Notifications
        .Where(n => n.UserId == userIdInt.Value && !n.IsRead && n.Intended == "customer")
        .ToList();

    foreach (var notification in notifications)
    {
        notification.IsRead = true; // Mark as read
    }

    // Save changes to the database
    _context.SaveChanges();


     _context.SaveChanges();

    return RedirectToAction("Dashboard"); // Redirect back to the dashboard
}





    }


   
}
