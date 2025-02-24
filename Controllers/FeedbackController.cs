using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using devDynast.Data;
using devDynast.Models;
using Microsoft.Extensions.Logging;

namespace devDynast.Controllers
{
    [Authorize]
    public class FeedbackController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(ApplicationDbContext context, ILogger<FeedbackController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Feedback/Create
        public IActionResult Feedback()
        {
            _logger.LogInformation("Navigated to Feedback/Create page.");
            return View();
        }

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Feedback(Feedback feedback)
{
    // Retrieve the user ID from the session
    var userId = HttpContext.Session.GetInt32("UserId");
    

    if (userId == null)
    {
        _logger.LogWarning("User not found in session.");
        return RedirectToAction("Error", "Home");
    }

    // Convert the nullable int to string
    feedback.UserId = userId.Value.ToString();

    if (ModelState.IsValid)
    {
        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index", "Home");
    }

    // Log the errors in the model state
    var errors = ModelState.Values.SelectMany(v => v.Errors);
    foreach (var error in errors)
    {
        _logger.LogWarning("Model error: {Error}", error.ErrorMessage);
    }

    return View(feedback);
}


    }
}
