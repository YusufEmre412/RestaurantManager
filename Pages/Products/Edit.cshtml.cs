using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using RestaurantManager.Models;

namespace RestaurantManager.Pages.Products
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Product Product { get; set; } = default!;

        [BindProperty]
        public List<IngredientSelection> IngredientSelections { get; set; } = new();

        public class IngredientSelection
        {
            public int InventoryId { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsSelected { get; set; }
            public int Quantity { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Ingredients)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }
            Product = product;
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");

            // Load ingredients
            var allInventory = await _context.Inventories.ToListAsync();
            IngredientSelections = allInventory.Select(i => {
                var existingIngredient = product.Ingredients.FirstOrDefault(pi => pi.IngredientId == i.ItemId);
                return new IngredientSelection
                {
                    InventoryId = i.ItemId,
                    Name = i.ItemName,
                    IsSelected = existingIngredient != null,
                    Quantity = existingIngredient?.Quantity ?? 1
                };
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Product.ProductInfo");
            
            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
                return Page();
            }

            _context.Attach(Product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                // Update ingredients
                var existingIngredients = await _context.ProductIngredients
                    .Where(pi => pi.ProductId == Product.ProductId)
                    .ToListAsync();
                
                _context.ProductIngredients.RemoveRange(existingIngredients);
                
                foreach (var selection in IngredientSelections)
                {
                    if (selection.IsSelected)
                    {
                        var productIngredient = new ProductIngredient
                        {
                            ProductId = Product.ProductId,
                            IngredientId = selection.InventoryId,
                            Quantity = selection.Quantity
                        };
                        _context.ProductIngredients.Add(productIngredient);
                    }
                }
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(Product.ProductId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
