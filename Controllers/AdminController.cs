using Art_BaBomb.Web.Models;
using Art_BaBomb.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Art_BaBomb.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();
            var model = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                model.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? "",
                    CurrentRole = roles.FirstOrDefault() ?? "No Role",
                    SelectedRole = roles.FirstOrDefault() ?? "Shopper"
                });
            }

            ViewBag.AvailableRoles = new List<string> { "Shopper", "Production", "Admin" };

            return View(model.OrderBy(u => u.Email).ToList());
        }

        // POST: Admin/UpdateRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(UserRoleViewModel model)
        {
            
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid role update request.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            var currentAdminEmail = User.Identity?.Name;

            if (string.Equals(user.Email, currentAdminEmail, StringComparison.OrdinalIgnoreCase)
                && model.SelectedRole != "Admin")
            {
                TempData["ErrorMessage"] = "You cannot remove your own Admin role.";
                return RedirectToAction(nameof(Users));
            }

            if (!await _roleManager.RoleExistsAsync(model.SelectedRole))
            {
                TempData["ErrorMessage"] = "Selected role does not exist.";
                return RedirectToAction(nameof(Users));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "Could not remove existing role.";
                    return RedirectToAction(nameof(Users));
                }
            }

            var addResult = await _userManager.AddToRoleAsync(user, model.SelectedRole);
            if (!addResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Could not assign selected role.";
                return RedirectToAction(nameof(Users));
            }

            TempData["SuccessMessage"] = $"Updated {user.Email} to role: {model.SelectedRole}";
            return RedirectToAction(nameof(Users));
        }
    }
}