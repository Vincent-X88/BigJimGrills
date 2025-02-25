using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using devDynast.Data;
using devDynast.Models;
using System.Globalization;
using devDynast.ViewModels;

namespace devDynast.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ILogger<UserController> _logger;

        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context, ILogger<UserController> logger) 
        {
            _context = context;
            _logger = logger; 
        }

        // GET: Admin/Index
        public IActionResult Index()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        // GET: Admin/EditUser/
        public IActionResult EditUser(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/EditUser/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(int id, [Bind("Id,FirstName,LastName,Email,PhoneNumber,Role")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(user);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Admin/DeleteUser/
        public IActionResult DeleteUser(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
          {
            var user = _context.Users.Find(id);
            if (user == null)
           {
            return NotFound();
           }

           _context.Users.Remove(user);
           _context.SaveChanges();
           return RedirectToAction(nameof(Index));
        }

        // GET: Admin/EditMeal/
        public IActionResult EditMeal(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var meal = _context.MenuItems.Find(id);
            if (meal == null)
            {
                return NotFound();
            }

            return View(meal);
        }

        // POST: Admin/EditMeal/
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult EditMeal(int id, [Bind("Id,Name,Category,Description,ImageUrl,Price")] MenuItem meal)
{
    
    Console.WriteLine($"Received ID: {id}, Meal ID: {meal.Id}");

    if (id != meal.Id)
    {
        Console.WriteLine("ID mismatch - returning NotFound.");
        return NotFound();
    }

    // Debugging: Check if the model state is valid
    Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

    if (ModelState.IsValid)
    {
        try
        {
            // Debugging: Before updating
            Console.WriteLine($"Updating meal: {meal.Name}, Category: {meal.Category}, Price: {meal.Price}");

            _context.Update(meal);
            var affectedRows = _context.SaveChanges();

            // Debugging: After saving changes
            Console.WriteLine($"SaveChanges() affected {affectedRows} rows.");

            if (affectedRows == 0)
            {
                Console.WriteLine("No rows affected. Possible concurrency issue.");
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            
            Console.WriteLine("Caught DbUpdateConcurrencyException.");
            Console.WriteLine($"Exception message: {ex.Message}");

            var databaseValues = _context.Entry(meal).GetDatabaseValues();

            if (databaseValues == null)
            {
                Console.WriteLine("Meal deleted by another user - returning error.");
                ModelState.AddModelError(string.Empty, "Unable to save changes. The meal was deleted by another user.");
                return RedirectToAction(nameof(MenuList));
            }
            else
            {
                var dbMeal = (Menu)databaseValues.ToObject();
                Console.WriteLine("Concurrency conflict - returning the current database values.");
                ModelState.AddModelError(string.Empty, "The meal was updated by another user. Your changes have been canceled.");
                return View(dbMeal);
            }
        }

        Console.WriteLine("Redirecting to MenuList.");
        return RedirectToAction(nameof(MenuList)); 
    }

    
    Console.WriteLine("Model state is invalid. Returning the view with the meal data.");
    return View(meal);
}


        // GET: Admin/DeleteMeal/
public IActionResult DeleteMeal(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var meal = _context.MenuItems.Find(id);
    if (meal == null)
    {
        return NotFound();
    }

    return View(meal);
}

// POST: Admin/DeleteMeal/
[HttpPost, ActionName("DeleteMeal")]
[ValidateAntiForgeryToken]
public IActionResult DeleteMealConfirmed(int id)
{
    var meal = _context.MenuItems.Find(id);
    if (meal == null)
    {
        return NotFound();
    }

    _context.MenuItems.Remove(meal);
    _context.SaveChanges();

    // Redirect to MenuList after deletion
    return RedirectToAction(nameof(MenuList));
}


        // GET: Admin/MenuList
        public IActionResult MenuList()
        {
            var meals = _context.MenuItems.ToList();
            return View(meals);
        }

        public IActionResult Dashboard()
{
    
    
    var unreadNotifications = _context.Notifications
        .Where(n => !n.IsRead && n.Intended == "admin")
        .ToList();

    
    ViewBag.UnreadNotificationsCount = unreadNotifications.Count;

    
    var notifications = unreadNotifications.Select(n => new NotificationViewModel
    {
        Message = n.Message,
        CreatedAt = n.CreatedAt
    }).ToList();

    return View(notifications);
}


        public IActionResult RegisterUser()
        {
            
            return View();
        }

        public IActionResult ShowMenuList()
        {
            
            return View();
        }

        public IActionResult UsersList()
        {
            
            return View();
        }

        public IActionResult Statistics()
        {
            
            return View();
        }

      public async Task<IActionResult> TopSellingItems()
{
    var report = await _context.Cart
        .Where(cart => cart.Status == "Order Collected")
        .GroupBy(cart => new 
        {
            Year = cart.CreatedAt.Year,
            Month = cart.CreatedAt.Month,
            ProductId = cart.ProductId
        })
        .Select(group => new 
        {
            Year = group.Key.Year,
            Month = group.Key.Month,
            ProductId = group.Key.ProductId,
            SalesCount = group.Sum(c => c.Quantity),
            TotalRevenue = group.Sum(c => c.Price * (double)c.Quantity)
        })
        .Join(_context.MenuItems,
            group => group.ProductId,
            menuItem => menuItem.Id.ToString(),
            (group, menuItem) => new TopSellingItemsViewModel
            {
                ItemName = menuItem.Name,
                SalesCount = group.SalesCount,
                TotalRevenue = group.TotalRevenue,
                Year = group.Year,
                Month = group.Month
            })
        .ToListAsync();

    // Group by Year and Month and take top 3 items for each month
    var topSellingItemsByMonth = report
        .GroupBy(item => new { item.Year, item.Month })
        .Select(monthGroup => new TopSellingItemsByMonthViewModel
        {
            MonthYear = new DateTime(monthGroup.Key.Year, monthGroup.Key.Month, 1).ToString("MMMM yyyy"),
            TopItems = monthGroup
                        .OrderByDescending(item => item.SalesCount)
                        .Take(3)
                        .ToList()
        })
        .ToList();

    return View(topSellingItemsByMonth);
}


[HttpGet("Admin/GetTopSellingItems")]
public async Task<IActionResult> GetTopSellingItems(int selectedMonth, int selectedYear)
{
    var report = await _context.Cart
        .Where(cart => cart.Status == "Order Collected" && 
                       cart.CreatedAt.Year == selectedYear && 
                       cart.CreatedAt.Month == selectedMonth)
        .GroupBy(cart => new 
        {
            ProductId = cart.ProductId
        })
        .Select(group => new 
        {
            ProductId = group.Key.ProductId,
            SalesCount = group.Sum(c => c.Quantity),
            TotalRevenue = group.Sum(c => c.Price * (double)c.Quantity)
        })
        .Join(_context.MenuItems,
            group => group.ProductId,
            menuItem => menuItem.Id.ToString(),
            (group, menuItem) => new TopSellingItemsViewModel
            {
                ItemName = menuItem.Name,
                SalesCount = group.SalesCount,
                TotalRevenue = group.TotalRevenue
            })
        .ToListAsync();

    // Prepare the response to send back to the client
    var response = new
    {
        MonthYear = new DateTime(selectedYear, selectedMonth, 1).ToString("MMMM yyyy"),
        TopItems = report
                    .OrderByDescending(item => item.SalesCount)
                    .ToList()
    };

    return Json(response);
}


         public async Task<IActionResult> SalesReport()
{
    var salesData = await _context.Cart
        .Where(c => c.Status == "Order Collected")
        .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
        .Select(g => new 
        {
            Year = g.Key.Year,
            Month = g.Key.Month,
            SalesCount = g.Sum(c => c.Quantity),
        })
        .ToListAsync();

    var salesDataViewModel = salesData
        .Select(data => new SalesDataViewModel
        {
            Month = new DateTime(data.Year, data.Month, 1).ToString("MMMM"),
            SalesCount = data.SalesCount
        })
        .OrderBy(sd => DateTime.ParseExact(sd.Month, "MMMM", CultureInfo.CurrentCulture).Month)
        .ToList();

    return View(salesDataViewModel);
}

[HttpGet]
public async Task<IActionResult> GetSalesDetails(string month)
{
    // Get the month number from the month name
    var monthNumber = DateTime.ParseExact(month, "MMMM", CultureInfo.CurrentCulture).Month;
    var year = DateTime.Now.Year; 

    // Fetch product details sold in the specified month
    var salesDetails = await _context.Cart
        .Where(c => c.Status == "Order Collected" && c.CreatedAt.Month == monthNumber && c.CreatedAt.Year == year)
        .GroupBy(c => c.ProductId) // Group by ProductId
        .Select(g => new
        {
            ProductId = g.Key, 
            Quantity = g.Sum(x => x.Quantity), // Sum of quantities
            Price = g.FirstOrDefault().Price 
        })
        .Select(g => new
        {
            Name = _context.MenuItems.FirstOrDefault(m => m.Id.ToString() == g.ProductId).Name, 
            Quantity = g.Quantity,
            Price = g.Price
        })
        .ToListAsync();

    // Find the day with the highest sales for this month
    var topDayData = await _context.Cart
        .Where(c => c.Status == "Order Collected" && c.CreatedAt.Month == monthNumber && c.CreatedAt.Year == year)
        .GroupBy(c => c.CreatedAt.Day)
        .Select(g => new 
        {
            Day = g.Key,
            TotalSales = g.Sum(c => c.Quantity) // Total sales for that day
        })
        .OrderByDescending(g => g.TotalSales)
        .FirstOrDefaultAsync(); 

    // Prepare response including TopDay and MaxSales
    return Json(new
    {
        month = month,
        products = salesDetails,
        topDay = topDayData?.Day ?? 0, 
        maxSales = topDayData?.TotalSales ?? 0 
    });
}


[HttpGet("Admin/GetCustomerPurchaseFrequency/{timeFrame}")]
public IActionResult GetCustomerPurchaseFrequency(string timeFrame)
{
    // Get all the cart items that have been marked as 'paid'
    var paidCarts = _context.Cart.Where(c => c.Status == "Order Collected");

    // Group by user and count their purchases
    var userPurchaseCounts = paidCarts.GroupBy(c => c.UserId)
        .Select(g => new
        {
            UserId = g.Key,
            PurchaseCount = g.Count(),
            LastPurchaseDate = g.Max(c => c.CreatedAt)
        }).ToList();

    // Set the thresholds for different customer segment
    int frequentThreshold = timeFrame == "weekly" ? 5 : timeFrame == "monthly" ? 15 : 50; 
    int occasionalThreshold = timeFrame == "weekly" ? 2 : timeFrame == "monthly" ? 6 : 20;

    var customerSegments = userPurchaseCounts.Select(u => new
    {
        u.UserId,
        u.PurchaseCount,
        Segment = u.PurchaseCount >= frequentThreshold
            ? "Frequent Buyer"
            : u.PurchaseCount >= occasionalThreshold
                ? "Occasional Buyer"
                : "Infrequent Buyer",
        u.LastPurchaseDate
    }).ToList();

    // Fetch user details from User table
    var userSegmentDetails = customerSegments.Join(
        _context.Users,
        cs => cs.UserId,
        u => u.Id.ToString(),
        (cs, u) => new
        {
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            cs.PurchaseCount,
            cs.Segment,
            cs.LastPurchaseDate
        }
    ).ToList();

    return Json(userSegmentDetails);
}

[HttpGet]
public IActionResult GetCustomerPurchaseFrequency()
{
    return View(); 
}


[HttpGet("salescompare")]
    public IActionResult SalesCompare()
    {
        return View();
    }


public async Task<List<SalesViewModel>> GetSalesData(string filterType)
{
    var query = _context.Cart
        .Where(c => c.Status == "Order Collected");

    // Use dynamic grouping
    var salesData = filterType switch
    {
        "day" => await query
            .GroupBy(c => c.CreatedAt.Date)
            .Select(g => new SalesViewModel
            {
                TimePeriod = g.Key.ToString("yyyy-MM-dd"),
                TotalSales = g.Sum(c => c.Quantity),
                TotalRevenue = g.Sum(c => c.Price * c.Quantity)
            })
            .ToListAsync(),

        "month" => await query
            .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month }) 
            .Select(g => new SalesViewModel
            {
                TimePeriod = $"{g.Key.Year}-{g.Key.Month:D2}",
                TotalSales = g.Sum(c => c.Quantity),
                TotalRevenue = g.Sum(c => c.Price * c.Quantity)
            })
            .ToListAsync(),

        "year" => await query
            .GroupBy(c => c.CreatedAt.Year)
            .Select(g => new SalesViewModel
            {
                TimePeriod = g.Key.ToString(),
                TotalSales = g.Sum(c => c.Quantity),
                TotalRevenue = g.Sum(c => c.Price * c.Quantity)
            })
            .ToListAsync(),

        _ => throw new ArgumentException("Invalid filter type. Use 'day', 'month', or 'year'.")
    };

    return salesData;
}




 [HttpGet("salesdata")]
public async Task<IActionResult> GetSales(string filterType)
{
    if (filterType != "day" && filterType != "month" && filterType != "year")
    {
        return BadRequest("Invalid filter type. Use 'day', 'month', or 'year'.");
    }

    var salesData = await GetSalesData(filterType);
    return Ok(salesData);
}

[HttpGet]
public IActionResult AllComplaints()
{
    var complaints = _context.Feedbacks
        .Select(f => new FeedbackViewModel
        {
            Id = f.Id,
            OrderNumber = f.OrderNumber,
            UserId = f.UserId,
            Content = f.Content,
            SubmittedAt = f.CreatedAt,
            IsAdminResponded = f.IsAdminResponded,
            AdminResponse = f.AdminResponse
        }).ToList();

    return View(complaints);
}

[HttpPost]
public IActionResult RespondToComplaint(int FeedbackId, string AdminResponse)
{
    var feedback = _context.Feedbacks.FirstOrDefault(f => f.Id == FeedbackId);

    if (feedback != null)
    {
        feedback.AdminResponse = AdminResponse;
        feedback.IsAdminResponded = true;
        _context.SaveChanges();
    }

    return RedirectToAction("AllComplaints");
}


public IActionResult ReviewedOrders()
{
    var ratings = _context.Ratings
        .Join(_context.Users,
            rating => rating.UserId,
            user => user.Id.ToString(),
            (rating, user) => new
            {
                rating.OrderNumber,
                rating.RatingValue,
                rating.Comment,
                rating.UserId,
                UserName = user.FirstName
            })
        .ToList();

    var reviewedOrders = new List<ReviewedOrderViewModel>();

    foreach (var rating in ratings)
    {
        // Fetch order items for the given order number
        var orderItems = _context.Cart
            .Where(c => c.OrderNumber == rating.OrderNumber)
            .Join(_context.MenuItems,
                cart => cart.ProductId,
                menuItem => menuItem.Id.ToString(),
                (cart, menuItem) => new OrderDetailsViewModel
                {
                    Id = cart.Id,
                    ProductName = menuItem.Name,
                    Category = menuItem.Category,
                    Price = cart.Price,
                    BasePrice = (float)menuItem.Price,
                    Quantity = cart.Quantity,
                    Status = cart.Status,
                    CreatedAt = cart.CreatedAt,
                    ProductImage = menuItem.ImageUrl,
                    OrderNumber = cart.OrderNumber,
                    ExtraIds = cart.ExtraIds 
                })
            .ToList();

        // Map extras to order items
        foreach (var orderItem in orderItems)
        {
            if (!string.IsNullOrEmpty(orderItem.ExtraIds))
            {
               
                var extraIds = orderItem.ExtraIds.Split(',').Select(int.Parse).ToList();
                var extras = _context.MenuIngredient
                    .Where(ingredient => extraIds.Contains(ingredient.Id))
                    .Select(ingredient => new ExtraViewModel
                    {
                        Id = ingredient.Id,
                        Name = ingredient.Name,
                        Price = ingredient.Price
                    })
                    .ToList();

                orderItem.Extras = extras; // Add extras to the order item
            }
        }

        // Add reviewed order to the list
        reviewedOrders.Add(new ReviewedOrderViewModel
        {
            OrderNumber = rating.OrderNumber,
            RatingValue = rating.RatingValue,
            Comment = rating.Comment,
            UserId = rating.UserId,
            UserName = rating.UserName,
            OrderItems = orderItems
        });
    }

    return View(reviewedOrders);
}

public IActionResult ClearNotifications()
{

    // Get all unread notifications for the user and mark them as read
    var notifications = _context.Notifications
        .Where(n => !n.IsRead && n.Intended == "admin")
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


public IActionResult GetNotifications()
{


    var unreadNotifications = _context.Notifications
       .Where(n => !n.IsRead && n.Intended == "admin")
       .ToList();


    ViewBag.UnreadNotificationsCount = unreadNotifications.Count;

    return View("NotificationBell", unreadNotifications);
}

// Display the list of ingredients for the admin to manage
    public IActionResult ViewIngredients()
    {
        var ingredients = _context.MenuIngredient.ToList(); // Fetch all ingredients
        return View(ingredients);
    }

    // Action to handle restocking an ingredient
    [HttpPost]
    public JsonResult Restock(int id, int quantity)
    {
        var ingredient = _context.MenuIngredient.FirstOrDefault(i => i.Id == id);
        if (ingredient == null)
        {
            return Json(new { success = false, message = "Ingredient not found." });
        }

        // Increase the ingredient's quantity
        ingredient.Quantity += quantity;
        _context.SaveChanges(); 

        return Json(new { success = true, message = "Restocked successfully!" });
    }




    }
}
