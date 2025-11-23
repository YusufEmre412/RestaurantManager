using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;

namespace RestaurantManager.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class MonthlyReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MonthlyReportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<MonthlySummary> MonthlySummaries { get; set; } = new List<MonthlySummary>();

        public async Task OnGetAsync()
        {
            // Get all paid orders with related data
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Ingredients)
                            .ThenInclude(pi => pi.Inventory)
                .Where(o => o.IsPaid)
                .ToListAsync();

            var summaries = orders
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new MonthlySummary
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1),
                    TotalRevenue = g.Sum(o => o.TotalFee),
                    TotalOrders = g.Count(),
                    TotalItemsSold = g.SelectMany(o => o.OrderItems).Sum(oi => oi.Count),
                    TotalIngredientsUsed = g.SelectMany(o => o.OrderItems)
                        .SelectMany(oi => oi.Product.Ingredients.Select(pi => pi.Quantity * oi.Count))
                        .Sum(),
                    ItemSales = g.SelectMany(o => o.OrderItems)
                        .GroupBy(oi => oi.Product.ProductName)
                        .ToDictionary(ig => ig.Key, ig => ig.Sum(oi => oi.Count)),
                    IngredientUsage = g.SelectMany(o => o.OrderItems)
                        .SelectMany(oi => oi.Product.Ingredients.Select(pi => new { Name = pi.Inventory.ItemName, Quantity = pi.Quantity * oi.Count }))
                        .GroupBy(x => x.Name)
                        .ToDictionary(ig => ig.Key, ig => ig.Sum(x => x.Quantity))
                })
                .OrderByDescending(s => s.Month)
                .ToList();

            MonthlySummaries = summaries;
        }
    }

    public class MonthlySummary
    {
        public DateTime Month { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalItemsSold { get; set; }
        public int TotalIngredientsUsed { get; set; }
        public Dictionary<string, int> ItemSales { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> IngredientUsage { get; set; } = new Dictionary<string, int>();
    }
}
