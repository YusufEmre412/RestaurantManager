using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Models;

namespace RestaurantManager.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roleNames = { "Admin", "Waiter" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            var adminEmail = "admin@restaurant.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    UserProfile = new UserProfile
                    {
                        Name = "Admin User",
                        Email = adminEmail,
                        Address = "Restaurant HQ",
                        Phone = "1234567890"
                    }
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            var waiterEmail = "waiter@restaurant.com";
            var waiterUser = await userManager.FindByEmailAsync(waiterEmail);

            if (waiterUser == null)
            {
                waiterUser = new ApplicationUser
                {
                    UserName = waiterEmail,
                    Email = waiterEmail,
                    EmailConfirmed = true,
                    UserProfile = new UserProfile
                    {
                        Name = "John Waiter",
                        Email = waiterEmail,
                        Address = "Restaurant Staff House",
                        Phone = "0987654321"
                    }
                };

                var result = await userManager.CreateAsync(waiterUser, "Waiter@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(waiterUser, "Waiter");
                }
            }

            // Seed Tables
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!await dbContext.Tables.AnyAsync())
            {
                var tables = new List<Table>();
                for (int i = 1; i <= 35; i++)
                {
                    tables.Add(new Table { TableNumber = i, IsOccupied = false });
                }
                await dbContext.Tables.AddRangeAsync(tables);
                await dbContext.SaveChangesAsync();
            }

            // Seed Categories and Products
            if (!await dbContext.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category { CategoryName = "Beverages" },
                    new Category { CategoryName = "Main Course" },
                    new Category { CategoryName = "Desserts" }
                };
                await dbContext.Categories.AddRangeAsync(categories);
                await dbContext.SaveChangesAsync();

                var products = new List<Product>
                {
                    new Product { ProductName = "Coke", Cost = 2.50m, CategoryId = categories[0].CategoryId },
                    new Product { ProductName = "Water", Cost = 1.00m, CategoryId = categories[0].CategoryId },
                    new Product { ProductName = "Burger", Cost = 12.00m, CategoryId = categories[1].CategoryId },
                    new Product { ProductName = "Pizza", Cost = 15.00m, CategoryId = categories[1].CategoryId },
                    new Product { ProductName = "Ice Cream", Cost = 5.00m, CategoryId = categories[2].CategoryId }
                };
                await dbContext.Products.AddRangeAsync(products);
                await dbContext.SaveChangesAsync();
            }

            // Seed Inventory
            var inventoryItems = new List<Inventory>
            {
                new Inventory { ItemName = "Hamburger Bun", ItemCount = 100, ItemCost = 0.50m },
                new Inventory { ItemName = "Hamburger Meat", ItemCount = 100, ItemCost = 2.00m },
                new Inventory { ItemName = "Lettuce", ItemCount = 50, ItemCost = 0.20m },
                new Inventory { ItemName = "Tomato", ItemCount = 50, ItemCost = 0.30m },
                new Inventory { ItemName = "Pickles", ItemCount = 50, ItemCost = 0.10m },
                new Inventory { ItemName = "Cheese", ItemCount = 50, ItemCost = 0.50m }
            };

            foreach (var item in inventoryItems)
            {
                if (!await dbContext.Inventories.AnyAsync(i => i.ItemName == item.ItemName))
                {
                    dbContext.Inventories.Add(item);
                }
            }
            await dbContext.SaveChangesAsync();

            // Seed System Config
            if (!await dbContext.SystemConfigs.AnyAsync())
            {
                dbContext.SystemConfigs.Add(new SystemConfig { Key = "IsRegisterOpen", Value = "True" });
                await dbContext.SaveChangesAsync();
            }

            // Seed Product Ingredients (Link Burger to Ingredients)
            var burger = await dbContext.Products.FirstOrDefaultAsync(p => p.ProductName == "Burger");
            if (burger != null)
            {
                // Ensure ingredients are linked
                var ingredientsToLink = new List<string> { "Hamburger Bun", "Hamburger Meat", "Lettuce", "Tomato" };
                
                foreach (var ingName in ingredientsToLink)
                {
                    var inventoryItem = await dbContext.Inventories.FirstOrDefaultAsync(i => i.ItemName == ingName);
                    if (inventoryItem != null)
                    {
                        if (!await dbContext.ProductIngredients.AnyAsync(pi => pi.ProductId == burger.ProductId && pi.IngredientId == inventoryItem.ItemId))
                        {
                            dbContext.ProductIngredients.Add(new ProductIngredient 
                            { 
                                ProductId = burger.ProductId, 
                                IngredientId = inventoryItem.ItemId, 
                                Quantity = 1 
                            });
                        }
                    }
                }
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
