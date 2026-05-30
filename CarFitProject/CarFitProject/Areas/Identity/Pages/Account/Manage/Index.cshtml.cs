#nullable disable

using System.ComponentModel.DataAnnotations;
using CarFitProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarFitProject.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Please enter your full name.")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
            [Display(Name = "Full name")]
            public string FullName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }
        }

        private void Load(ApplicationUser user)
        {
            Username = user.UserName;
            Input = new InputModel
            {
                FullName = user.FullName,
                Email = user.Email
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Unable to load user.");

            Load(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Unable to load user.");

            if (!ModelState.IsValid)
            {
                Username = user.UserName;
                return Page();
            }

            if (Input.FullName != user.FullName)
            {
                user.FullName = Input.FullName;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    Username = user.UserName;
                    return Page();
                }
            }

            var currentEmail = await _userManager.GetEmailAsync(user);
            if (!string.Equals(Input.Email, currentEmail, StringComparison.OrdinalIgnoreCase))
            {
                var setEmail = await _userManager.SetEmailAsync(user, Input.Email);
                var setUserName = await _userManager.SetUserNameAsync(user, Input.Email);
                if (!setEmail.Succeeded || !setUserName.Succeeded)
                {
                    foreach (var error in setEmail.Errors.Concat(setUserName.Errors))
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    Username = user.UserName;
                    return Page();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated.";
            return RedirectToPage();
        }
    }
}
