using System;
using System.Net;
using devDynast.Models;
using devDynast.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using devDynast.ViewModels;
using Microsoft.Extensions.Logging; 
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO; 
using System.Net.Mail;


public class CartController : Controller
{
     private readonly ApplicationDbContext _context;
    private readonly ILogger<CartController> _logger; 

    public CartController(ApplicationDbContext context, ILogger<CartController> logger) 
    {
        _context = context;
        _logger = logger; 
    }

    [HttpPost]
    public ActionResult AddToCart(string productId, decimal price, List<int> extraIds, string note) // Added note parameter
{
    try
    {
        // Retrieve the user ID from the session as int
        var userIdInt = HttpContext.Session.GetInt32("UserId");
        var userId = userIdInt?.ToString();

        // Convert the list of extra IDs to a comma-separated string
        string extraIdsString = extraIds != null ? string.Join(",", extraIds) : null;

        var cartItem = new Cart
        {
            UserId = userId,
            ProductId = productId,
            Quantity = 1,
            Price = (double)price,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            OrderNumber = "0",
            ExtraIds = extraIdsString,
            SpecialNote = note // Save the special note
        };

        // Add the cart item to the context
        _context.Cart.Add(cartItem);
        _context.SaveChanges();

        return Json(new { success = true, message = "Item added to cart." });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = "Error adding item to cart: " + ex.Message });
    }
}

public IActionResult Cart()
{
    var userIdInt = HttpContext.Session.GetInt32("UserId");
    var userId = userIdInt?.ToString();

    // Create an instance of MenuViewModel
    var model = new MenuViewModel();

    if (!string.IsNullOrEmpty(userId))
    {
        _logger.LogInformation($"User ID retrieved from session: {userId}");
        
        // Load cart items
        var cartItems = _context.Cart
            .Where(c => c.UserId == userId && c.Status == "pending")
            .Join(_context.MenuItems,
                  cart => cart.ProductId,
                  menuItem => menuItem.Id.ToString(),
                  (cart, menuItem) => new CartItemViewModel
                  {
                      Id = cart.Id,
                      Quantity = cart.Quantity,
                      Price = cart.Price,
                      ProductName = menuItem.Name,
                      ProductImage = menuItem.ImageUrl,
                      Category = menuItem.Category,
                      SpecialNote = cart.SpecialNote,
                      // Initialize SelectedExtras as an empty list
                      SelectedExtras = new List<ExtraViewModel>(),
                      ExtraIds = cart.ExtraIds 
                  })
            .ToList();

      
        foreach (var item in cartItems)
        {
            if (!string.IsNullOrEmpty(item.ExtraIds)) 
            {
                // Split the extra_ids string into an array of IDs
                var extraIds = item.ExtraIds.Split(',');

                // Fetch extras based on the parsed IDs
                item.SelectedExtras = extraIds
                    .Select(extraId => int.TryParse(extraId.Trim(), out var id) ? id : (int?)null) 
                    .Where(id => id.HasValue) 
                    .Join(_context.MenuIngredient, // Join with MenuIngredient to get extra details
                          id => id.Value,
                          extra => extra.Id,
                          (id, extra) => new ExtraViewModel
                          {
                              Id = extra.Id,
                              Name = extra.Name,
                              Price = extra.Price
                          })
                    .ToList();
            }
        }

        _logger.LogInformation($"Number of items found in cart: {cartItems.Count}");

        foreach (var item in cartItems)
        {
           
            _logger.LogInformation($"Cart Item ID: {item.Id}, Product Name: {item.ProductName}, Quantity: {item.Quantity}, Price: {item.Price:F2}");
            if (item.SelectedExtras != null && item.SelectedExtras.Any())
            {
                _logger.LogInformation($"Selected Extras for Item ID {item.Id}: {string.Join(", ", item.SelectedExtras.Select(e => $"{e.Name} (+R{e.Price:F2})"))}");
            }
            else
            {
                _logger.LogInformation($"No extras found for Item ID {item.Id}.");
            }
        }

        if (cartItems.Count > 0)
        {
            model.CartItems = cartItems;
            _logger.LogInformation("Cart items successfully added to model.");
        }
        else
        {
            _logger.LogWarning("No items found in the cart for the user.");
        }

        return View("~/Views/User/Cart.cshtml", model);
    }
    else
    {
        _logger.LogWarning("User ID is null. Redirecting to login.");
        return RedirectToAction("Login", "Account");
    }
}


[HttpPost]
public IActionResult RemoveFromCart(int id)
{
    // Retrieve the user ID from the session
    var userIdInt = HttpContext.Session.GetInt32("UserId");
    var userId = userIdInt?.ToString();


    if (!string.IsNullOrEmpty(userId))
    {
        var cartItem = _context.Cart.FirstOrDefault(c => c.Id == id && c.UserId == userId);
        
        if (cartItem != null)
        {
            _context.Cart.Remove(cartItem); 
            _context.SaveChanges(); // Save changes to the database
            return Json(new { success = true });
        }
        else
        {
            return Json(new { success = false, message = "Item not found." });
        }
    }
    
    return Json(new { success = false, message = "User ID is null." });
}

[HttpPost]
public IActionResult UpdateQuantity(int id, int quantity)
{
    
    var userIdInt = HttpContext.Session.GetInt32("UserId");
    var userId = userIdInt?.ToString();

    // Check if the userId is not null
    if (!string.IsNullOrEmpty(userId))
    {
        var cartItem = _context.Cart.FirstOrDefault(c => c.Id == id && c.UserId == userId);
        
        if (cartItem != null)
        {
            cartItem.Quantity = quantity; // Update the quantity
            _context.SaveChanges();
            return Json(new { success = true });
        }
        else
        {
            return Json(new { success = false, message = "Item not found." });
        }
    }
    
    return Json(new { success = false, message = "User ID is null." });
}

public IActionResult Checkout(DateTime? scheduledPickupDate, TimeSpan? scheduledPickupTime, bool isScheduled)
{
    var userIdInt = HttpContext.Session.GetInt32("UserId");
    var userId = userIdInt?.ToString();

    // Log the incoming values to check if they are correctly passed
    _logger.LogInformation("Checkout initiated.");
    _logger.LogInformation($"Scheduled: {isScheduled}");

   
          DateTime? utcDate = null;
    
    if (!string.IsNullOrEmpty(userId))
    {
        _logger.LogInformation($"User ID retrieved from session: {userId}");

        // Retrieve cart items and join with menu items to get product details
        var cartItems = _context.Cart
            .Where(c => c.UserId == userId && c.Status == "pending")
            .Join(_context.MenuItems,
                cart => cart.ProductId,
                menuItem => menuItem.Id.ToString(),
                (cart, menuItem) => new CartItemViewModel
                {
                    CartItemId = cart.Id,
                    MenuItemId = menuItem.Id,
                    Quantity = cart.Quantity,
                    Price = cart.Price,
                    ExtraIds = cart.ExtraIds,
                    ProductName = menuItem.Name,
                    ProductImage = menuItem.ImageUrl,
                    Category = menuItem.Category,
                    SelectedExtras = new List<ExtraViewModel>()
                })
            .ToList(); // Fetch the results into memory

        foreach (var item in cartItems)
        {
            if (!string.IsNullOrEmpty(item.ExtraIds))
            {
                var extraIds = item.ExtraIds.Split(',', StringSplitOptions.None)
                    .Select(extraId => int.TryParse(extraId.Trim(), out var id) ? id : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

                // Retrieve the extras from the database based on parsed IDs
                item.SelectedExtras = _context.MenuIngredient
                    .Where(extra => extraIds.Contains(extra.Id))
                    .Select(extra => new ExtraViewModel
                    {
                        Id = extra.Id,
                        Name = extra.Name,
                        Price = extra.Price
                    })
                    .ToList();
            }
        }

        if (cartItems.Any())
        {
            // Generate a unique order number
            var orderNumber = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            // Calculate total price and other details for the notification
            var totalItems = cartItems.Count;
            var totalPrice = cartItems.Sum(c => c.Quantity * c.Price);

    
            if (isScheduled)
            {
                
                if (scheduledPickupDate.HasValue)
    {
        // Convert the date to UTC before saving it to the database.
         utcDate = DateTime.SpecifyKind(scheduledPickupDate.Value, DateTimeKind.Utc);
        _logger.LogInformation($"Scheduled Pickup Date (UTC): {utcDate}");

    }
                foreach (var item in cartItems)
                {
                    var cartItem = _context.Cart.Find(item.CartItemId);
                    cartItem.IsScheduled = true; // Set scheduled flag
                    cartItem.ScheduledPickupDate = utcDate;
                    cartItem.ScheduledPickupTime = scheduledPickupTime;
                }
            }

            // Update the status of each cart item and decrease ingredient stock
            foreach (var item in cartItems)
            {
                var cartItem = _context.Cart.Find(item.CartItemId);
                cartItem.Status = "Order Approved";
                cartItem.OrderNumber = orderNumber;

                if (!string.IsNullOrEmpty(item.ExtraIds))
                {
                    var extraIds = item.ExtraIds.Split(',')
                        .Select(id => int.Parse(id))
                        .ToList();

                    // Retrieve and update ingredients based on the extracted IDs
                    var ingredients = _context.MenuIngredient
                        .Where(ingredient => extraIds.Contains(ingredient.Id))
                        .ToList();

                    // Subtract stock for each ingredient based on the quantity ordered
                    foreach (var ingredient in ingredients)
                    {
                        var previousQuantity = ingredient.Quantity;
                        ingredient.Quantity -= item.Quantity;

                        if (ingredient.Quantity < 0)
                        {
                            ingredient.Quantity = 0;
                        }

                        _logger.LogInformation($"Ingredient: {ingredient.Name}, Previous Quantity: {previousQuantity}, Updated Quantity: {ingredient.Quantity}");
                        _context.MenuIngredient.Update(ingredient);
                    }
                }
            }

            _context.SaveChanges(); // Save changes to the database

            // Send a notification after checkout
            var notificationMessage = $"A new order (Order No: {orderNumber}) has been placed with {totalItems}.";
            var notification = new Notification
            {
                Message = notificationMessage,
                UserId = userIdInt.Value,
                CreatedAt = DateTime.UtcNow,
                OrderId = orderNumber,
                Intended = "admin"
            };

            _context.Notifications.Add(notification);

            // Prepare notification for the customer
var notificationMessageForCustomer = $"Your order (Order No: {orderNumber}) has been approved";
var notificationForCustomer = new Notification
{
    Message = notificationMessageForCustomer,
    UserId = userIdInt.Value, // Customer's user ID
    CreatedAt = DateTime.UtcNow,
    OrderId = orderNumber,
    Intended = "customer"
};

// Save the customer notification
_context.Notifications.Add(notificationForCustomer);
            _context.SaveChanges();

            // Generate the receip
            var receiptPath = GenerateReceiptPdf(userId, cartItems, totalPrice, orderNumber, isScheduled, utcDate, scheduledPickupTime);


            // Get the email address of the user who placed the order
            var userEmail = _context.Users
                .Where(u => u.Id.ToString() == userId)
                .Select(u => u.Email)
                .FirstOrDefault();

            // Send email with receipt
            try
            {
                using (var client = new SmtpClient("smtp.gmail.com", 587))
                {
                    client.Credentials = new NetworkCredential("bigjimgrills19@gmail.com", "amzp fued evxt yvaz");
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress("bigjimgrills@gmail.com"),
                        Subject = "Your Order Receipt",
                        Body = "Thank you for your order! Please find your receipt attached.",
                        IsBodyHtml = false // Set to true if you're sending HTML
                    };
                    
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        mailMessage.To.Add(userEmail); // Use the user's email found in the User table
                        mailMessage.Attachments.Add(new Attachment(receiptPath));

                        client.Send(mailMessage);
                    }
                    else
                    {
                        _logger.LogWarning("User email not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error if the email sending fails
                _logger.LogError($"Failed to send email: {ex.Message}");
            }
            return Json(new { success = true, message = "Checkout successful!", orderNumber = orderNumber, receiptUrl = Url.Action("ViewReceipt", new { receiptPath = receiptPath }) });
        }
    }

    return Json(new { success = false, message = "No items in the cart." });
}

private int GetOrderPositionInQueue(string orderNumber)
{
    // Fetch unique order numbers with "Order Approved" status from the database
    var approvedOrders = _context.Cart
                                .Where(o => o.Status == "Order Approved")
                                .GroupBy(o => o.OrderNumber)
                                .OrderBy(g => g.First().CreatedAt) // Order by the creation time of the first item in the group
                                .Select(g => g.Key) // Select unique order numbers
                                .ToList();

    // Get the position of the current order in the queue
    var currentOrderIndex = approvedOrders.FindIndex(o => o == orderNumber) + 1; // FindIndex is 0-based, so we add 1

    return currentOrderIndex > 0 ? currentOrderIndex : approvedOrders.Count + 1; // If not found, return last position
}


public TimeSpan CalculateCollectionTime(List<CartItemViewModel> cartItems, string orderNumber)
{

    
    // Fetch unique orders with "Order Approved" status from the database
    var approvedOrders = _context.Cart
        .Where(o => o.Status == "Order Approved")
        .GroupBy(o => o.OrderNumber)
        .OrderBy(g => g.First().CreatedAt) // Order by the creation time of the first item in the group
        .Select(g => g.First()) 
        .ToList();

    // Get the position of the current order in the queue
    var currentOrderIndex = approvedOrders.FindIndex(o => o.OrderNumber == orderNumber) + 1; // FindIndex is 0-based, so we add 1

    // Total orders ahead in the queue
    int totalOrdersAhead = currentOrderIndex - 1;

    // Base collection time (depending on the type and quantity of the items in the current order)
    TimeSpan baseCollectionTime = TimeSpan.Zero;

    // Weights for different categories
    TimeSpan pizzaTimePerItem = TimeSpan.FromMinutes(10);
    TimeSpan burgerTimePerItem = TimeSpan.FromMinutes(8);
    TimeSpan sandwichTimePerItem = TimeSpan.FromMinutes(5);
    TimeSpan friesTimePerItem = TimeSpan.FromMinutes(3);
    TimeSpan drinkTimePerItem = TimeSpan.FromMinutes(1);

    // Calculate base time for the current order
    foreach (var item in cartItems)
    {
        switch (item.Category.ToLower())
        {
            case "pizzas":
                baseCollectionTime += pizzaTimePerItem * item.Quantity;
                break;
            case "burgers":
                baseCollectionTime += burgerTimePerItem * item.Quantity;
                break;
            case "sandwiches":
                baseCollectionTime += sandwichTimePerItem * item.Quantity;
                break;
            case "fries":
                baseCollectionTime += friesTimePerItem * item.Quantity;
                break;
            case "drinks":
                baseCollectionTime += drinkTimePerItem * item.Quantity;
                break;
            default:
                break;
        }
    }

    // Now adjust based on total unique orders ahead and their content
    TimeSpan additionalTimeForQueue = TimeSpan.Zero;

    foreach (var order in approvedOrders.Take(totalOrdersAhead))
    {
        // Join Cart with MenuItems to get the Category for each item in the order
        var itemsInOrder = _context.Cart
            .Where(c => c.OrderNumber == order.OrderNumber) 
            .Join(_context.MenuItems,
                  cart => cart.ProductId,
                  menuItem => menuItem.Id.ToString(),
                  (cart, menuItem) => new CartItemViewModel
                  {
                      Quantity = cart.Quantity,
                      Category = menuItem.Category,
                  })
            .ToList();

        foreach (var item in itemsInOrder)
        {
            switch (item.Category.ToLower())
            {
                case "pizzas":
                    additionalTimeForQueue += pizzaTimePerItem * item.Quantity;
                    break;
                case "burgers":
                    additionalTimeForQueue += burgerTimePerItem * item.Quantity;
                    break;
                case "sandwiches":
                    additionalTimeForQueue += sandwichTimePerItem * item.Quantity;
                    break;
                case "fries":
                    additionalTimeForQueue += friesTimePerItem * item.Quantity;
                    break;
                case "drinks":
                    additionalTimeForQueue += drinkTimePerItem * item.Quantity;
                    break;
                default:
                    break;
            }
        }
    }

    // The total collection time is the base time for this order plus the additional queue time
    TimeSpan totalCollectionTime = baseCollectionTime.Add(additionalTimeForQueue);

    return totalCollectionTime;
}
private string GenerateReceiptPdf(string userId, List<CartItemViewModel> cartItems, double totalPrice, string orderNumber, bool isScheduled, DateTime? scheduledPickupDate, TimeSpan? scheduledPickupTime)
{
    var receiptPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "receipts", $"receipt_{userId}_{DateTime.UtcNow.Ticks}.pdf");

    var (firstName, lastName) = GetUserDetails(userId);

    // Get South Africa Standard Time (UTC+2)
    TimeZoneInfo southAfricaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");

    DateTime collectionDateTime;
    string timeDifferenceText;

    if (isScheduled && scheduledPickupDate.HasValue && scheduledPickupTime.HasValue)
    {
        // Use the scheduled date and time (convert to South African time zone)
        collectionDateTime = TimeZoneInfo.ConvertTimeFromUtc(scheduledPickupDate.Value.Add(scheduledPickupTime.Value), southAfricaTimeZone);

        timeDifferenceText = "Scheduled for the selected time.";
    }
    else
    {
        // If not scheduled, calculate the default collection time
        TimeSpan collectionTime = CalculateCollectionTime(cartItems, orderNumber);

        // Get current time in UTC
        DateTime orderDateTimeUtc = DateTime.UtcNow;
        DateTime orderDateTime = TimeZoneInfo.ConvertTimeFromUtc(orderDateTimeUtc, southAfricaTimeZone);

        // Calculate collection date and time in South African time zone
        collectionDateTime = orderDateTime.Add(collectionTime);

       
        TimeSpan timeUntilCollection = collectionDateTime - orderDateTime;

        // Convert the time difference into hours and minutes for display
        timeDifferenceText = $"{(int)timeUntilCollection.TotalHours} hour(s) and {timeUntilCollection.Minutes} minute(s) from now";
    }

    // Fetch approved orders to calculate the number in the queue and the current position
    var approvedOrders = _context.Cart
        .Where(o => o.Status == "Order Approved")
        .GroupBy(o => o.OrderNumber)
        .Select(g => g.First())
        .ToList();

    // Total unique orders in the queue
    int totalOrdersInQueue = approvedOrders.Count;

    // Set the current order index to be the next position in the queue
    int currentOrderIndex = totalOrdersInQueue + 1; // New order is always at the end

    using (FileStream fs = new FileStream(receiptPath, FileMode.Create))
    using (var doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 50, 50, 25, 25)) // Setting margins
    {
        var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(doc, fs);
        doc.Open();

        // Set up fonts
        var titleFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 18, iTextSharp.text.BaseColor.BLACK);
        var subtitleFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA_BOLD, 12, iTextSharp.text.BaseColor.GRAY);
        var bodyFont = iTextSharp.text.FontFactory.GetFont(iTextSharp.text.FontFactory.HELVETICA, 10, iTextSharp.text.BaseColor.BLACK);

        // Add a title
        var title = new iTextSharp.text.Paragraph("Order Receipt", titleFont) { Alignment = iTextSharp.text.Element.ALIGN_CENTER };
        doc.Add(title);

        // Add space
        doc.Add(new iTextSharp.text.Paragraph("\n"));

        // Add order details in a table format
        var orderDetailsTable = new iTextSharp.text.pdf.PdfPTable(2);
        orderDetailsTable.WidthPercentage = 100;
        orderDetailsTable.SetWidths(new float[] { 30f, 70f });

        orderDetailsTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph("Order Number:", subtitleFont)) { Border = 0 });
        orderDetailsTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph(orderNumber, bodyFont)) { Border = 0 });

        orderDetailsTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph("Date:", subtitleFont)) { Border = 0 });
        // Get South African Standard Time (UTC+2)
TimeZoneInfo southAfricaTimeZoe = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");

// Convert the current UTC time to South African Standard Time
DateTime southAfricaTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, southAfricaTimeZoe);

// Add the South African time to the order details
orderDetailsTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph(southAfricaTime.ToString("yyyy-MM-dd HH:mm:ss"), bodyFont)) { Border = 0 });


        orderDetailsTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph("Customer Name:", subtitleFont)) { Border = 0 });
        orderDetailsTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph($"{firstName} {lastName}", bodyFont)) { Border = 0 });

        // Add collection time to the order details
        orderDetailsTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph("Collection Time:", subtitleFont)) { Border = 0 });
        orderDetailsTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph($"{collectionDateTime:yyyy-MM-dd HH:mm:ss} ({timeDifferenceText})", bodyFont)) { Border = 0 });

        // Add queue details
        orderDetailsTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph("Orders in Queue:", subtitleFont)) { Border = 0 });
        orderDetailsTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph($"{totalOrdersInQueue}", bodyFont)) { Border = 0 });

        orderDetailsTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph("Your Position in Queue:", subtitleFont)) { Border = 0 });
        orderDetailsTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph($"{currentOrderIndex} of {currentOrderIndex}", bodyFont)) { Border = 0 });

        doc.Add(orderDetailsTable);

        // Add space before listing items
        doc.Add(new iTextSharp.text.Paragraph("\n"));

        // Add a table for the items
        var itemTable = new iTextSharp.text.pdf.PdfPTable(4);
        itemTable.WidthPercentage = 100;
        itemTable.SetWidths(new float[] { 50f, 15f, 15f, 20f });

        // Add table headers
        itemTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph("Product Name", subtitleFont)) { BackgroundColor = iTextSharp.text.BaseColor.LIGHT_GRAY });
        itemTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph("Quantity", subtitleFont)) { BackgroundColor = iTextSharp.text.BaseColor.LIGHT_GRAY });
        itemTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph("Unit Price", subtitleFont)) { BackgroundColor = iTextSharp.text.BaseColor.LIGHT_GRAY });
        itemTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph("Total", subtitleFont)) { BackgroundColor = iTextSharp.text.BaseColor.LIGHT_GRAY });

        double finalTotalPrice = 0;

        // Add each cart item
        foreach (var item in cartItems)
        {
            double itemTotal = item.Quantity * item.Price;

            // Add each item
            itemTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph(item.ProductName, bodyFont)));
            itemTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph(item.Quantity.ToString(), bodyFont)));
            itemTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph($"R{item.Price:F2}", bodyFont)));
            itemTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph($"R{itemTotal:F2}", bodyFont)));

            // Check if there are any extras for the item
            if (item.SelectedExtras != null && item.SelectedExtras.Any())
            {
                // Add extra ingredients row
                itemTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph("Extras:", bodyFont)) { Colspan = 4, Border = 0 });

                
                foreach (var extra in item.SelectedExtras)
                {
                    itemTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph(extra.Name, bodyFont)) { Colspan = 2, Border = 0 });
                    itemTable.AddCell(new iTextSharp.text.pdf.PdfPCell(new iTextSharp.text.Paragraph($"R{extra.Price:F2}", bodyFont)) { Colspan = 2, Border = 0 });

                    
                    itemTotal += extra.Price;
                }
            }

            finalTotalPrice += itemTotal; 
        }


        doc.Add(itemTable);

        // Add space before total
        doc.Add(new iTextSharp.text.Paragraph("\n"));

        var totalParagraph = new iTextSharp.text.Paragraph($"Total Price: R{finalTotalPrice:F2}", subtitleFont);
        totalParagraph.Alignment = iTextSharp.text.Element.ALIGN_RIGHT;
        doc.Add(totalParagraph);

        doc.Add(new iTextSharp.text.Paragraph("\n"));
        doc.Add(new iTextSharp.text.Paragraph("Thank you for your order!", bodyFont) { Alignment = iTextSharp.text.Element.ALIGN_CENTER });

        doc.Close();
    }

    return receiptPath.Replace("\\", "/");
}


public User GetUserById(string userId)
    {
        
        if (int.TryParse(userId, out int id))
        {
            return _context.Users.FirstOrDefault(u => u.Id == id);
        }
        return null;
    }

    private (string FirstName, string LastName) GetUserDetails(string userId)
{
    var user = GetUserById(userId); 
    return (user?.FirstName ?? "N/A", user?.LastName ?? "N/A");
}

public IActionResult ViewReceipt(string receiptPath)
{
    if (!string.IsNullOrEmpty(receiptPath))
    {
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", receiptPath);

        if (System.IO.File.Exists(fullPath))
        {
            
            var fileBytes = System.IO.File.ReadAllBytes(fullPath);
            return File(fileBytes, "application/pdf", "Receipt.pdf");
        }
    }

    return NotFound("Receipt not found.");
}


[HttpPost]
public IActionResult MarkNotificationsAsRead()
{
    
    var notifications = _context.Notifications
        .Where(n => !n.IsRead)
        .ToList();

    foreach (var notification in notifications)
    {
        notification.IsRead = true;
        notification.OrderId = "1";
    }

    _context.SaveChanges();

    return Json(new { success = true });
}


public IActionResult GetOrderDetails()
{
    // Fetch paid orders and group them by orderNumber
    var groupedOrders = _context.Cart
        .Where(c => c.Status == "Order Approved")
        .Join(_context.MenuItems,
              cart => cart.ProductId,
              menuItem => menuItem.Id.ToString(),
              (cart, menuItem) => new
              {
                  cart.OrderNumber,
                  cart.Quantity,
                  cart.Price,
                  cart.Status,
                  cart.ExtraIds,
                  cart.SpecialNote // Fetch the special note
              })
        .ToList() // Fetch the data first
        .GroupBy(order => order.OrderNumber)
        .Select(g => new OrderSummaryViewModel
        {
            OrderNumber = g.Key,
            TotalQuantity = g.Sum(x => x.Quantity),
            TotalPrice = g.Sum(x =>
            {
                // Base product price
                double basePrice = x.Price;
                double totalExtras = 0;

                if (!string.IsNullOrEmpty(x.ExtraIds))
                {
                    var extraIds = x.ExtraIds.Split(',')
                        .Select(int.Parse)
                        .ToList();

                    // Fetch the prices of the extras
                    totalExtras = _context.MenuIngredient
                        .Where(ingredient => extraIds.Contains(ingredient.Id))
                        .Sum(ingredient => ingredient.Price);
                }

                // Calculate the total price considering quantity
                return (basePrice + totalExtras) * x.Quantity;
            }),
            Status = g.First().Status,
            SpecialNote = g.First().SpecialNote 
        })
        .ToList();

    return View("~/Views/Admin/OrderDetails.cshtml", groupedOrders);
}


[HttpPost]
public IActionResult UpdateOrderStatus([FromForm] string orderNumber, [FromForm] string status)
{
    
    // Fetch all cart items for the given order number
    var cartItems = _context.Cart.Where(c => c.OrderNumber == orderNumber).ToList();
    
    if (cartItems.Any())
    {
        
        var userId = cartItems.First().UserId;

        // Update the status for all items with this order number
        foreach (var cartItem in cartItems)
        {
            cartItem.Status = status;
        }

        _context.SaveChanges(); // Save the changes to the database
        _logger.LogInformation("Order with OrderNumber: {OrderNumber} status updated to {Status}", orderNumber, status);

        if (!string.IsNullOrEmpty(userId))
{
    int parsedUserId;
    if (int.TryParse(userId, out parsedUserId))
    {
        var notificationMessage = $"Status for Order No: {orderNumber} has been updated to {status}.";
        var notification = new Notification
        {
            Message = notificationMessage,
            UserId = parsedUserId, 
            CreatedAt = DateTime.UtcNow,
            OrderId = orderNumber,
            Intended = "customer"
        };

        _context.Notifications.Add(notification);
        _context.SaveChanges(); // Save the notifications
    }
    else
    {
        _logger.LogWarning("Failed to parse userId: {UserId}", userId);
    }
}
else
{
    _logger.LogWarning("userId is null or empty");
}

        return RedirectToAction("GetOrderDetails");
    }
    else
    {
        
        _logger.LogWarning("Order with OrderNumber: {OrderNumber} not found", orderNumber);
        return RedirectToAction("GetOrderDetails", new { error = "Order not found" });
    }
}

  public IActionResult OrderDetails(string orderNumber)
{
    // Fetch order items for the given order number
    var orderItems = _context.Cart
        .Where(c => c.OrderNumber == orderNumber)
        .Join(_context.MenuItems,
              cart => cart.ProductId,
              menuItem => menuItem.Id.ToString(),
              (cart, menuItem) => new OrderDetailsViewModel
              {
                  Id = cart.Id,
                  ProductName = menuItem.Name,
                  Category = menuItem.Category,
                  BasePrice = cart.Price,
                  Quantity = cart.Quantity,
                  Status = cart.Status,
                  CreatedAt = cart.CreatedAt,
                  ProductImage = menuItem.ImageUrl,
                  OrderNumber = cart.OrderNumber,
                  ExtraIds = cart.ExtraIds,
                  SpecialNote = cart.SpecialNote 
              })
        .ToList();  // Fetch data into memory

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
            item.Extras = new List<ExtraViewModel>();  // No extras for this item
        }
    }

    return View("~/Views/Admin/OrderDetail.cshtml", orderItems);
}



[HttpPost]
public JsonResult RemoveExtra(int itemId, int extraId)
{
    // Find the cart item by itemId
    var cartItem = _context.Cart
        .FirstOrDefault(ci => ci.Id == itemId);
    
    if (cartItem != null)
    {
        
        var extraIds = cartItem.ExtraIds.Split(',')
                                         .Select(int.Parse)
                                         .ToList();

        
        if (extraIds.Contains(extraId))
        {
            // Remove the extraId from the list
            extraIds.Remove(extraId);
            
            
            cartItem.ExtraIds = string.Join(",", extraIds);

            // Save changes to the database
            _context.SaveChanges();

            return Json(new { success = true, message = "Extra removed successfully!" });
        }
        else
        {
            return Json(new { success = false, message = "Extra not found." });
        }
    }

    return Json(new { success = false, message = "Cart item not found." });
}


public ActionResult Details(int id)
{
    // Get the selected product by ID
    var product = _context.MenuItems.SingleOrDefault(p => p.Id == id);

    
    if (product == null)
    {
        return NotFound();
    }

    // Get extras from the database based on product category
    var extras = _context.MenuIngredient.Where(e => e.Category == product.Category).ToList();

    // Check if any extra has a quantity of 0
    bool isAvailable = extras.All(e => e.Quantity > 0);  // True if all ingredients have quantity > 0

    var model = new ProductDetailsViewModel
    {
        Product = product,
        Extras = extras,
        IsAvailable = isAvailable  // Pass availability status to the view
    };

    return View("~/Views/User/Details.cshtml", model);
}

public ActionResult Payment(decimal total, string isScheduled, string date = null, string time = null)
{
    // Manually parse the isScheduled string to a boolean
    bool isScheduledBool = !string.IsNullOrEmpty(isScheduled) && isScheduled.ToLower() == "true";

    var model = new PaymentViewModel
    {
        TotalAmount = total,
        IsScheduled = isScheduledBool,
        ScheduledDate = date,
        ScheduledTime = time
    };

    return View("~/Views/User/Payment.cshtml", model);
}






}









