
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VERSUM.KitchenWareERP.Models;

namespace VERSUM.KitchenWareERP.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        private readonly SignInManager<IdentityUser> _signInManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // ==========================================
        // LOGIN PAGE - GET
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin") ||
                    User.IsInRole("Staff"))
                {
                    return RedirectToAction("Index", "Home");
                }

                return RedirectToAction(nameof(AccessDenied));
            }

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };

            return View(model);
        }

        // ==========================================
        // LOGIN PROCESS - POST
        // ==========================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");

                return View(model);
            }

            // Only Admin and Staff accounts can access ERP

            var isAdmin = await _userManager.IsInRoleAsync(
                user, "Admin");

            var isStaff = await _userManager.IsInRoleAsync(
                user, "Staff");

            if (!isAdmin && !isStaff)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");

                return View(model);
            }

            // Validate password and create login session

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Prevent unsafe external URL redirection

                if (!string.IsNullOrWhiteSpace(model.ReturnUrl) &&
                    Url.IsLocalUrl(model.ReturnUrl))
                {
                    return LocalRedirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            // Display unsuccessful login message

            ModelState.AddModelError(
                string.Empty,
                "Unable to sign in. Check your credentials " +
                "or try again later.");

            return View(model);
        }

        // ==========================================
        // STAFF REGISTRATION PAGE - GET
        // ==========================================

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // ==========================================
        // CREATE STAFF ACCOUNT - POST
        // ==========================================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();

            // Check whether email already exists

            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An account with this email already exists.");

                return View(model);
            }

            // Verify that the Staff role exists

            if (!await _roleManager.RoleExistsAsync("Staff"))
            {
                _logger.LogError(
                    "Staff role has not been initialized.");

                ModelState.AddModelError(
                    string.Empty,
                    "Staff registration is currently unavailable. " +
                    "Please contact the administrator.");

                return View(model);
            }

            // Create new Staff user

            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                LockoutEnabled = true
            };

            var result = await _userManager.CreateAsync(
                user,
                model.Password);

            // Handle account creation errors

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            // Assign Staff role automatically

            var roleResult =
                await _userManager.AddToRoleAsync(
                    user, "Staff");

            if (!roleResult.Succeeded)
            {
                // Remove incomplete account

                var cleanupResult =
                    await _userManager.DeleteAsync(user);

                if (!cleanupResult.Succeeded)
                {
                    _logger.LogError(
                        "Failed to clean up incomplete Staff " +
                        "account with ID {UserId}.",
                        user.Id);
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to assign Staff role. " +
                    "Please contact the administrator.");

                return View(model);
            }

            // Display successful registration message

            TempData["Success"] =
                "Staff account created successfully!";

            return RedirectToAction(nameof(Register));
        }

        // ==========================================
        // UPDATED SECURE LOGOUT
        // ==========================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(nameof(Login));
        }

        // ==========================================
        // ACCESS DENIED PAGE
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
