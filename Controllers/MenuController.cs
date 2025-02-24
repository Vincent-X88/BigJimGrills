using Microsoft.AspNetCore.Mvc;
using devDynast.Data;
using devDynast.Models;
using devDynast.ViewModels;

namespace devDynast.Controllers
{
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MenuController> _logger;

        public MenuController(ApplicationDbContext context, ILogger<MenuController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Menu/AddMeal
        public IActionResult AddMeal()
        {
            return View();
        }

        // POST: Menu/AddMeal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMeal(Menu meal)
        {
            if (ModelState.IsValid)
            {
                _context.Menus.Add(meal);
                _context.SaveChanges();
                return RedirectToAction("Index", "Home");
            }
            return View(meal);
        }

        // GET: Menu/AddMenuItem
        public IActionResult AddMenuItem()
        {
            return View();
        }

        

public IActionResult Index(string category)
{
    // Fetch all menus
    var menus = _context.Menus.ToList();

    // Fetch MenuItems based on the selected category, if provided
    var menuItems = string.IsNullOrEmpty(category) 
        ? _context.MenuItems.ToList() 
        : _context.MenuItems.Where(m => m.Category == category).ToList();

    var viewModel = new MenuViewModel
    {
        Menus = menus,
        MenuItems = menuItems 
    };

   return View("~/Views/Home/Index.cshtml", viewModel);
}




        // POST: Menu/AddMenuItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMenuItem(MenuItem meal)
        {
            if (ModelState.IsValid)
            {
                _context.MenuItems.Add(meal);
                _context.SaveChanges();
                return RedirectToAction("Index", "Home");
            }
            return View(meal);
        }
    }
}
