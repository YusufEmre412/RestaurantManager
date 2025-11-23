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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            
            IngredientSelections = _context.Inventories.Select(i => new IngredientSelection
            {
                InventoryId = i.ItemId,
                Name = i.ItemName,
                IsSelected = false,
                Quantity = 1
            }).ToList();

            return Page();
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

        public async Task<IActionResult> OnPostAsync()
        {
            // Remove ProductInfo validation errors if any, as we might not be setting it here
            ModelState.Remove("Product.ProductInfo");

            if (!ModelState.IsValid)
            {
                ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
                return Page();
            }

            _context.Products.Add(Product);
            await _context.SaveChangesAsync();

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

            return RedirectToPage("./Index");
        }
    }
}
