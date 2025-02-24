using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using devDynast.Data;
using devDynast.Models;
using System.Linq;
using System.Threading.Tasks;

namespace devDynast.Controllers
{
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> SalesReport()
{
    var salesData = await _context.Cart
        .Where(c => c.Status == "paid")
        .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
        .Select(g => new 
        {
            Year = g.Key.Year,
            Month = g.Key.Month,
            SalesCount = g.Count()
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

    }
}
