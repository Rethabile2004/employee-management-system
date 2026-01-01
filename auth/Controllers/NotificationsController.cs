using auth.Models.Models;
using Auth.Models.Models;
using Auth.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Streamline.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly DbServices<NotificationModel> _dbService;
        //private readonly SessionHelper _sessionHelper;
        private readonly DbServices<UserModel> _userService;

        
        public NotificationsController()
        {
            _userService = new DbServices<UserModel>("users");
            _dbService = new DbServices<NotificationModel>("notifications");
            //_sessionHelper = new SessionHelper(accessor);
        }

       
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var user = await _userService.GetByFieldAsync("userId", userId);
            

            var notifications = await _dbService.GetAllAsync();
            var userNotifications = notifications.Where(n => n.UserId == userId).ToList();
            var filteredotifications = userNotifications.Where((n) => n.SentBy=="Hr"|| n.SentBy == "Hr").Select((n) => n);
            return View(filteredotifications);
        }
      
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Invalid notification ID.");

            var notification = await _dbService.GetByIdAsync(id);
            if (notification == null)
                return NotFound();
            notification.SentDate = DateTime.SpecifyKind(notification.SentDate, DateTimeKind.Utc);
            if (notification.ReadStatus != "Read")
            {
                notification.ReadStatus = "Read";
                await _dbService.UpdateAsync(notification.NotificationId, notification);
            }

            return View(notification);
        }
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Invalid notification ID.");

            var notification = await _dbService.GetByIdAsync(id);
            if (notification == null)
                return NotFound();
            notification.SentDate = DateTime.SpecifyKind(notification.SentDate, DateTimeKind.Utc);
            notification.ReadStatus = notification.ReadStatus == "Read" ? "Unread" : "Read";
            await _dbService.UpdateAsync(notification.NotificationId, notification);

            TempData["SuccessMessage"] = notification.ReadStatus == "Read"
                ? "Notification marked as read."
                : "Notification marked as unread.";

            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> RefreshUnread()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var allNotifications = await _dbService.GetAllAsync();
            var unread = allNotifications
                .Where(n => n.SentBy == "Hr"&&n.UserId== userId /*&& n.ReadStatus == "Unread"*/)
                .OrderByDescending(n => n.SentDate)
                .Take(5)
                .ToList();
            
            return PartialView("_UnreadNotificationsPartial", unread);
        }
        //[HttpGet]
        //public async Task<IActionResult> RefreshList()
        //{
        //    var notifications = await _dbService.GetAllAsync();
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var userNotifications = notifications
        //        .FindAll(n => n.UserId == userId)
        //        .OrderByDescending(n => n.SentDate)
        //        .ToList();
        //    var orderedNotifications= userNotifications.OrderByDescending(n => n.SentDate)
        //        .ToList();

        //    return PartialView("_NotificationListPartial", orderedNotifications);
        //}
    }
}