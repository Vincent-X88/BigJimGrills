using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using devDynast.Models;
using devDynast.Data;
using Microsoft.AspNetCore.Authorization;
using devDynast.Controllers;

namespace devDynast.Controllers;

[Authorize]
public class HomeController : Controller
{

    private readonly ApplicationDbContext _context;

    private readonly ILogger<HomeController> _logger; // Add logger

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
            
        {
            _context = context;
            _logger = logger; // Initialize the logger
        }
    public IActionResult Index()
    {
        
        return View();
    }

    public IActionResult Privacy()
    {
    
        return View();
    }

    public async Task<IActionResult> MenuTabs()
{
    
    var menus =  _context.Menus.ToList();

    if (menus == null || !menus.Any())
    {
       
        menus = new List<Menu>();
    }
    else
    {
       
    }

    return View(menus);
}


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Redirect to the menu page
    public IActionResult Menu()
    {
        return View("~/Views/User/Menu.cshtml"); 
    }

    // Redirect to the dashboard page
    public IActionResult Dashboard()
    {
        return RedirectToAction("Dashboard", "User");
    }

    
}


