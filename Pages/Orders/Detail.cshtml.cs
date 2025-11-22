using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;

namespace RestaurantManager.Pages.Orders
{
    [Authorize]
    public class DetailModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int TableId { get; set; }

        public Table Table { get; set; } = default!;
        public Order? CurrentOrder { get; set; }
        public IList<Category> Categories { get; set; } = new List<Category>();

        public async Task<IActionResult> OnGetAsync()
        {
            Table = await _context.Tables.FindAsync(TableId);

            if (Table == null)
            {
                return NotFound();
            }

            CurrentOrder = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.TableId == TableId && !o.IsPaid);

            Categories = await _context.Categories
                .Include(c => c.Products)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCreateOrderAsync()
        {
            var table = await _context.Tables.FindAsync(TableId);
            if (table == null) return NotFound();

            if (table.IsOccupied) return RedirectToPage(); // Already has order?

            var order = new Order
            {
                TableId = TableId,
                IsPaid = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            table.IsOccupied = true;
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddItemAsync(int productId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.TableId == TableId && !o.IsPaid);

            if (order == null) return BadRequest("No active order");

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return BadRequest("Product not found");

            var existingItem = order.OrderItems.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Count++;
                existingItem.SubTotal = existingItem.Count * product.Cost;
            }
            else
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = productId,
                    Count = 1,
                    SubTotal = product.Cost,
                    UserId = User.Identity?.Name
                });
            }

            order.TotalFee = order.OrderItems.Sum(i => i.SubTotal);
            await _context.SaveChangesAsync();

            // Return partial view for order summary
            CurrentOrder = order;
            return Partial("_OrderSummary", this);
        }
        
        public async Task<IActionResult> OnPostPayOrderAsync()
        {
             var order = await _context.Orders.FirstOrDefaultAsync(o => o.TableId == TableId && !o.IsPaid);
             if (order != null)
             {
                 order.IsPaid = true;
                 var table = await _context.Tables.FindAsync(TableId);
                 if (table != null) table.IsOccupied = false;
                 await _context.SaveChangesAsync();
             }
             return RedirectToPage("./Index");
        }
    }
}
