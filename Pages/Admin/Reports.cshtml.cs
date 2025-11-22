using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;

namespace RestaurantManager.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ReportsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReportsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public IList<Order> RecentOrders { get; set; } = new List<Order>();

        public async Task OnGetAsync()
        {
            TotalSales = await _context.Orders
                .Where(o => o.IsPaid)
                .SumAsync(o => o.TotalFee);

            TotalOrders = await _context.Orders.CountAsync();

            RecentOrders = await _context.Orders
                .Include(o => o.Table)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();
        }
    }
}
