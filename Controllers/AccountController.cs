using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VIUUniversity.Models;
using VIUUniversity.ViewModels.Account;

namespace VIUUniversity.Controllers
{
    // [AllowAnonymous] is applied per action, not on the class: a class-level attribute
    // would also cancel the [Authorize] on Logout (ASP0026).
    [Route("tai-khoan")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet("dang-nhap")]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost("dang-nhap")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.PublicForms)]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
                return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");

            if (result.IsLockedOut)
                ModelState.AddModelError(string.Empty, "Tài khoản tạm khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau.");
            else
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");

            return View(model);
        }

        [AllowAnonymous]
        [HttpGet("dang-ky")]
        public IActionResult Register() => View(new RegisterViewModel());

        [AllowAnonymous]
        [HttpPost("dang-ky")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitPolicies.PublicForms)]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, Roles.Student);
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("New account registered: {Email}", model.Email);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpPost("dang-xuat")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpGet("khong-co-quyen")]
        public IActionResult AccessDenied() => View();
    }
}