using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace YourApp.Controllers
{
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<AccountController> logger)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet] public IActionResult Login() => View();
        [HttpGet] public IActionResult Register() => View();

        // LOGIN: Verify Firebase token → Sign in
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> FirebaseLogin([FromBody] FirebaseTokenModel model)
        {
            if (string.IsNullOrEmpty(model.idToken))
                return Json(new { success = false, message = "No token" });

            try
            {
                // Verify Firebase token
                FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(model.idToken);
                string uid = decodedToken.Uid;
                string email = decodedToken.Claims["email"]?.ToString();

                if (string.IsNullOrEmpty(email))
                    return Json(new { success = false, message = "No email in token" });

                // Find or create ASP.NET Identity user
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true  // CRITICAL: Allow login
                    };
                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                        return Json(new { success = false, message = "Failed to create user" });
                }
                else
                {
                    // Ensure EmailConfirmed is true (in case old user)
                    if (!user.EmailConfirmed)
                    {
                        user.EmailConfirmed = true;
                        await _userManager.UpdateAsync(user);
                    }
                }

                // Sign in with ASP.NET Identity
                await _signInManager.SignInAsync(user, isPersistent: false);

                // ---- ADD CLAIMS (replaces SessionHelper) ----
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email!),
                    new Claim(ClaimTypes.Role, "Employee") // or get from Firestore later
                };

                var identity = new ClaimsIdentity(claims, "Firebase");
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync("Identity.Application", principal);
                return Json(new { success = true });
            }
            catch (FirebaseAuthException ex)
            {
                return Json(new { success = false, message = "Invalid token: " + ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server error: " + ex.Message });
            }
        }
        // REGISTER: Create in Firebase (frontend) → sync to ASP.NET
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> FirebaseRegister([FromBody] FirebaseTokenModel model)
        {
            try
            {
                FirebaseToken decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(model.idToken);
                string email = decoded.Claims["email"].ToString();

                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                    return Json(new { success = false, message = "User already exists" });

                user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user);
                return Json(new { success = result.Succeeded });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
    }

    public class FirebaseTokenModel
    {
        public string idToken { get; set; } = string.Empty;
    }
}