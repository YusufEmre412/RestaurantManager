using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Models;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EditUserModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EditUserModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public SelectList Roles { get; set; } = default!;

        public class InputModel
        {
            [Required]
            public string Id { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            public string Role { get; set; } = string.Empty;

            [Required]
            public string Name { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            Input = new InputModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                Name = user.UserProfile?.Name ?? "",
                Role = roles.FirstOrDefault() ?? ""
            };

            Roles = new SelectList(_roleManager.Roles.Select(r => r.Name).ToList());
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Roles = new SelectList(_roleManager.Roles.Select(r => r.Name).ToList());
                return Page();
            }

            var user = await _userManager.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Id == Input.Id);

            if (user == null)
            {
                return NotFound();
            }

            user.Email = Input.Email;
            user.UserName = Input.Email;
            
            if (user.UserProfile == null)
            {
                user.UserProfile = new UserProfile();
            }
            user.UserProfile.Name = Input.Name;
            user.UserProfile.Email = Input.Email;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                var resultRole = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                
                if (resultRole.Succeeded)
                {
                     if (!string.IsNullOrEmpty(Input.Role) && await _roleManager.RoleExistsAsync(Input.Role))
                    {
                        await _userManager.AddToRoleAsync(user, Input.Role);
                    }
                }

                return RedirectToPage("./Users");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            Roles = new SelectList(_roleManager.Roles.Select(r => r.Name).ToList());
            return Page();
        }
    }
}
