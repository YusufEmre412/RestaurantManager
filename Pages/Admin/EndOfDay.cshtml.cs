using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;

namespace RestaurantManager.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EndOfDayModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EndOfDayModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalItemsSold { get; set; }
        public IList<Order> TodayOrders { get; set; } = new List<Order>();

        public bool IsRegisterOpen { get; set; }

        public Dictionary<string, int> ItemSales { get; set; } = new Dictionary<string, int>();

        public DateTime? SessionStart { get; set; }

        public async Task OnGetAsync()
        {
            // Get register status
            var config = await _context.SystemConfigs.FindAsync("IsRegisterOpen");
            IsRegisterOpen = config != null && bool.Parse(config.Value);

            // Get last register open time
            var lastOpenConfig = await _context.SystemConfigs.FindAsync("LastRegisterOpenTime");
            DateTime startTime = DateTime.UtcNow.Date; // Default to today if not set
            
            if (lastOpenConfig != null && DateTime.TryParse(lastOpenConfig.Value, out DateTime parsedTime))
            {
                startTime = parsedTime;
                SessionStart = parsedTime;
            }

            TodayOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.CreatedAt >= startTime && o.IsPaid)
                .ToListAsync();

            TotalOrders = TodayOrders.Count;
            TotalRevenue = TodayOrders.Sum(o => o.TotalFee);
            TotalItemsSold = TodayOrders.SelectMany(o => o.OrderItems).Sum(i => i.Count);

            // Calculate Item Sales Breakdown
            ItemSales = TodayOrders.SelectMany(o => o.OrderItems)
                .GroupBy(i => i.Product.ProductName)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Count));
        }

        public async Task<IActionResult> OnPostToggleRegisterAsync(string action)
        {
            var config = await _context.SystemConfigs.FindAsync("IsRegisterOpen");
            if (config == null)
            {
                config = new SystemConfig { Key = "IsRegisterOpen", Value = "True" };
                _context.SystemConfigs.Add(config);
            }

            if (action == "open")
            {
                // Update start time
                var lastOpenConfig = await _context.SystemConfigs.FindAsync("LastRegisterOpenTime");
                var now = DateTime.UtcNow.ToString("o");
                
                if (lastOpenConfig == null)
                {
                    lastOpenConfig = new SystemConfig { Key = "LastRegisterOpenTime", Value = now };
                    _context.SystemConfigs.Add(lastOpenConfig);
                }
                else
                {
                    lastOpenConfig.Value = now;
                }

                config.Value = "True";
            }
            else if (action == "close")
            {
                config.Value = "False";
            }
            
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}
