using auth.Models;
using Auth.Models.Models;
using Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace auth.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DbServices<UserModel> _userService;
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            _userService = new DbServices<UserModel>("users");
        }

        
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //return Content(userId);
            var existing = await _userService.GetByFieldAsync("userId", userId!);
            if (existing == null)
            {
                return RedirectToAction("Dashboard", "Employee");
            }

            if (existing!.Role == "Admin")
            {
            return RedirectToAction("Dashboard","Admin");

            }else if(existing!.Role == "Hr")
            {
                return RedirectToAction("Dashboard", "HR");
            }
            else
            {
                return RedirectToAction("Dashboard", "Employee");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
