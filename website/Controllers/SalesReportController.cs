using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using devDynast.Models;
using devDynast.Data;


namespace devDynast.Controllers
{
    public class SalesReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> TopSellingItemsReport()
        {
            var report = await _context.Cart
                .Where(cart => cart.Status == "paid")
                .GroupBy(cart => cart.ProductId)
                .Select(group => new 
                {
                    ProductId = group.Key,
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

            return View(report);
        }
    }
}
