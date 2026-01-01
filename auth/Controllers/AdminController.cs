using auth.Models.Models;
using Auth.Models.Models;
using Auth.Models.NewFolder;
using Auth.Models.ViewModels;
using Auth.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auth.Controllers
{
    public class AdminController : Controller
    {
        private readonly DbServices<UserModel> _userService;
        private readonly DbServices<AuditModel> _auditService;
        private readonly DbServices<SkillModel> _skillService;
        private readonly DbServices<QualificationModel> _qualificationService;
        private readonly DbServices<TrainingModel> _trainingService;
        private readonly DbServices<NotificationModel> _notificationService;

        public AdminController()
        {
            _userService = new DbServices<UserModel>("users");
            _auditService = new DbServices<AuditModel>("audits");
            _skillService = new DbServices<SkillModel>("skills");
            _qualificationService = new DbServices<QualificationModel>("qualifications");
            _trainingService = new DbServices<TrainingModel>("training");
            _notificationService = new DbServices<NotificationModel>("notifications");
        }

        //private string GetCurrentUserName()
        //{
        //    return .GetString("FullName") ?? "";
        //}

        private async Task<string> GetCurrentUserIdAsync()
        {
            var userId= User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            return userId;    
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var existing = await _userService.GetByFieldAsync("userId", userId);

            if (existing == null)
                return NotFound("User not found.");

            return View(existing);
        }

        public async Task<IActionResult> Users()
        {
            ViewBag.LoggedInName = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var allUsers = await _userService.GetAllAsync();
            var employees = allUsers?.Where(u => u.Role == "Employee" && u.Status == "Approved").ToList() ?? new List<UserModel>();

            var allAudits = await _auditService.GetAllAsync();
            var allTrainings = await _trainingService.GetAllAsync();

            var userList = employees.Select(u =>
            {
                var userAudits = allAudits.Where(a => a.UserId == u.UserId).ToList();
                var userTrainings = allTrainings.Where(t => t.UserId == u.UserId).ToList();

                int completed = userTrainings.Count(t => t.CompletionStatus?.ToLower() == "completed");
                int pending = userTrainings.Count(t => t.CompletionStatus?.ToLower() == "pending");
                string trainingStatus;

                if (completed == 0 && pending == 0)
                    trainingStatus = "Unknown";
                else if (pending == 0)
                    trainingStatus = "Completed";
                else if (completed > 0 && pending > 0)
                    trainingStatus = "In Progress";
                else
                    trainingStatus = "Pending";

                return new UserHRViewModel
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    NumberOfReports = userAudits.Count,
                    TrainingsCompleted = completed,
                    TrainingsPending = pending,
                    TrainingStatus = trainingStatus,
                    AuditCount = userAudits.Count,
                    AccountStatus = u.Status
                };
            }).ToList();

            return View(userList);
        }

        public async Task<IActionResult> UserDetails(string id)
        {
            ViewBag.LoggedInName = GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var users = await _userService.GetAllAsync();
            var user = users.FirstOrDefault(found => found.UserId == id);
            if (user == null)
                return NotFound();

            var skills = (await _skillService.GetAllAsync()).Where(s => s.UserId == id).ToList();
            var trainings = (await _trainingService.GetAllAsync()).Where(t => t.UserId == id).ToList();
            var qualifications = (await _qualificationService.GetAllAsync()).Where(q => q.UserId == id).ToList();

            var viewModel = new UserDetailsViewModel
            {
                User = user,
                Skills = skills,
                Trainings = trainings,
                Qualifications = qualifications
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Audits()
        {
            ViewBag.LoggedInName = GetCurrentUserIdAsync();
            var audits = await _auditService.GetAllAsync();
            var auditList = audits.Select(a => new AuditViewModel
            {
                AuditId = a.AuditId,
                UserId = a.UserId,
                Date = a.Date,
                AuditedBy = a.AuditedBy,
                Action = a.Action,
            }).OrderByDescending(a => a.Date).ToList();

            return View(auditList);
        }

        public async Task<IActionResult> AuditDetails(string auditId)
        {
            ViewBag.LoggedInName = GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(auditId))
                return NotFound();

            var audit = await _auditService.GetByIdAsync(auditId);
            if (audit == null)
                return NotFound();

            var viewModel = new AuditViewModel
            {
                AuditId = audit.AuditId,
                UserId = audit.UserId,
                Date = audit.Date,
                AuditedBy = audit.AuditedBy,
                Action=audit.Action
            };

            var user = await _userService.GetByIdAsync(audit.UserId);
            ViewBag.UserFullName = user?.FullName ?? "Unknown";

            return View(viewModel);
        }

        public async Task<IActionResult> Accounts()
        {
            ViewBag.LoggedInName = GetCurrentUserIdAsync();
            var allUsers = await _userService.GetAllAsync();
            var pending = allUsers?.Where(u => u.Status == "Pending").ToList() ?? new List<UserModel>();

            var list = pending.Select(u => new UserViewModel
            {
                UserId = u.UserId,
                FullName = u.FullName
            }).ToList();

            return View(list);
        }
        [HttpGet]
        public async Task<IActionResult> RefreshUnread()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var allNotifications = await _notificationService.GetAllAsync();

            return PartialView("_UnreadNotificationsPartial", allNotifications);
        }
        public async Task<IActionResult> Notifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }
            var user = await _userService.GetByFieldAsync("userId", userId);


            var notifications = await _notificationService.GetAllAsync();

            var filteredotifications = notifications.Where((n) => n.SentBy == "Admin").Select((n) => n);
            return View(filteredotifications);
        }
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            //return Content(id);
            if (string.IsNullOrEmpty(id))
                return BadRequest("Invalid notification ID.");

            var notification = await _notificationService.GetByIdAsync(id);
            if (notification == null)
                return NotFound();
            notification.SentDate = DateTime.SpecifyKind(notification.SentDate, DateTimeKind.Utc);
            notification.ReadStatus = notification.ReadStatus == "Read" ? "Unread" : "Read";
            await _notificationService.UpdateAsync(notification.NotificationId, notification);

            TempData["SuccessMessage"] = notification.ReadStatus == "Read"
                ? "Notification marked as read."
                : "Notification marked as unread.";

            return RedirectToAction("Notifications");
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateAccount(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID is required.");

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            user.Status = "Pending";
            await _userService.UpdateAsync(userId, user);
            var userInfor = await _userService.GetByFieldAsync("userId", userId);
            AuditModel newAudit = new()
            {
                Action = "deactivate account",
                AuditedBy = userInfor!.FullName,
                AuditId = Guid.NewGuid().ToString(),
                Date = DateTime.UtcNow,
                Details = $"Deactivate user with {userId} on ${DateTime.UtcNow}",
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };
            //var user = await _userService.GetByFieldAsync("userId", userId);
            //string helperText = nameof(SkillModel);

            await _auditService.CreateAsync(newAudit.AuditId, newAudit);
            var notification = new NotificationModel
            {
                NotificationId = Guid.NewGuid().ToString(),
                Message = $"Your account has been approved.",
                ReadStatus = "Unread",
                SentBy = "Admin",
                SentDate = DateTime.UtcNow,
                UserId = userId
            };

            TempData["SuccessMessage"] = "Skill added successfully!";
            await _notificationService.CreateAsync(notification.NotificationId, notification);
            TempData["Success"] = $"User {user.FullName} has been approved.";
            return RedirectToAction("Accounts");
        }

        [HttpPost]
        public async Task<IActionResult> ApproveAccount(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID is required.");

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            user.Status = "Approved";
            await _userService.UpdateAsync(userId, user);
            var userInfor = await _userService.GetByFieldAsync("userId", userId);
            AuditModel newAudit = new()
            {
                Action = "approve user",
                AuditedBy = userInfor!.FullName,
                AuditId = Guid.NewGuid().ToString(),
                Date = DateTime.UtcNow,
                Details = $"Approve user with {userId} on ${DateTime.UtcNow}",
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };
            //var user = await _userService.GetByFieldAsync("userId", userId);
            //string helperText = nameof(SkillModel);

            await _auditService.CreateAsync(newAudit.AuditId, newAudit);
            var notification = new NotificationModel
            {
                NotificationId = Guid.NewGuid().ToString(),
                Message = $"Your account has been approved.",
                ReadStatus = "Unread",
                SentBy = "Admin",
                SentDate = DateTime.UtcNow,
                UserId = userId
            };

            TempData["SuccessMessage"] = "Skill added successfully!";
            await _notificationService.CreateAsync(notification.NotificationId, notification);
            TempData["Success"] = $"User {user.FullName} has been approved.";
            return RedirectToAction("Accounts");
        }

        //public async Task<IActionResult> Notifications()
        //{
        //    ViewBag.LoggedInName = GetCurrentUserIdAsync();
        //    var currentUserId = GetCurrentUserIdAsync();
        //    if (string.IsNullOrEmpty(User.FindFirstValue(ClaimTypes.NameIdentifier)))
        //        return RedirectToAction("Login", "Account");

        //    var allNotifications = await _notificationService.GetAllAsync();
        //    var userNotifications = allNotifications.Where(n => n.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier)).ToList();
        //    return View(userNotifications);
        //}

        public async Task<IActionResult> Profile()
        {
            var currentUserId = GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(User.FindFirstValue(ClaimTypes.NameIdentifier)))
                return RedirectToAction("Login", "Account");

            var user = await _userService.GetByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (user == null)
                return NotFound();

            return View(user);
        }
    }
}