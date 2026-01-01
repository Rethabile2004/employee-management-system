using auth.Models.Models;
using Auth.Models.Models;
using Auth.Models.ViewModel;
using Auth.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Streamline.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly DbServices<UserModel> _dbService;
        private readonly DbServices<SkillModel> _dbSkillService;
        private readonly DbServices<TrainingModel> _dbTrainingService;
        private readonly DbServices<QualificationModel> _dbQualificationService;
        private readonly DbServices<NotificationModel> _notificationService;
        //private readonly SessionHelper _sessionHelper;

        public EmployeeController(IHttpContextAccessor accessor)
        {
            _notificationService = new DbServices<NotificationModel>("notifications");
            _dbSkillService = new DbServices<SkillModel>("skills");
            _dbTrainingService = new DbServices<TrainingModel>("trainings");
            _dbQualificationService = new DbServices<QualificationModel>("qualifications");
            _dbService = new DbServices<UserModel>("users");
            //_sessionHelper = new SessionHelper(accessor);
        }
        [HttpPost]
        public async Task<IActionResult> Add(UserModel model, IFormFile? profileFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || model.UserId != userId)
                return Unauthorized();


            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Any())
                    .ToDictionary(x => x.Key, x => x.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                Console.WriteLine("ModelState Errors: " + System.Text.Json.JsonSerializer.Serialize(errors));
                return View(model);
            }

            try
            {

                //var existing = await _dbService.GetByFieldAsync("userId", userId);
                //if (existing == null) return View("NotFound"); ;
                model.DateUpdated = DateTime.UtcNow;
                if (model.DateAdded.Kind != DateTimeKind.Utc)
                    model.DateAdded = model.DateAdded.ToUniversalTime();
                if (model.LastLogin != default && model.LastLogin.Kind != DateTimeKind.Utc)
                    model.LastLogin = model.LastLogin.ToUniversalTime();
                string newPictureFileName = model.ProfilePicture;
                await _dbService.CreateAsync(userId, model);
                return RedirectToAction("Index","Home");

                if (profileFile != null && profileFile.Length > 0)
                {

                    var allowed = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                    if (!allowed.Contains(profileFile.ContentType.ToLowerInvariant()))
                    {
                        ModelState.AddModelError("profileFile", "Only JPG, PNG, GIF, WebP allowed.");
                        return View(model);
                    }


                    var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    Directory.CreateDirectory(folder);


                    var guid8 = Guid.NewGuid().ToString("N")[..8];
                    var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                    var ext = Path.GetExtension(profileFile.FileName).ToLowerInvariant();
                    var fileName = $"{userId}_{guid8}_{timestamp}{ext}";
                    var fullPath = Path.Combine(folder, fileName);

                    await using (var stream = new FileStream(fullPath, FileMode.Create))
                        await profileFile.CopyToAsync(stream);


                    if (!string.IsNullOrWhiteSpace(model.ProfilePicture))
                    {
                        var oldPath = Path.Combine(folder, model.ProfilePicture);
                        if (System.IO.File.Exists(oldPath) && !oldPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                            System.IO.File.Delete(oldPath);
                    }

                    newPictureFileName = fileName;
                }

                model.FullName = model.FullName?.Trim();
                model.Email = model.Email?.Trim();
                model.Position = model.Position?.Trim();
                model.ProfilePicture = newPictureFileName;


                model.DateUpdated = DateTime.UtcNow;
                if (model.DateAdded.Kind != DateTimeKind.Utc)
                    model.DateAdded = model.DateAdded.ToUniversalTime();
                if (model.LastLogin != default && model.LastLogin.Kind != DateTimeKind.Utc)
                    model.LastLogin = model.LastLogin.ToUniversalTime();

                await _dbService.CreateAsync(userId, model);

                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Edit POST] Firestore error: {ex}");
                ModelState.AddModelError("", "Failed to save profile. Check server logs.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                var user = await _dbService.GetByFieldAsync("userId", userId);
                if (user == null)
                    return View("Add", new UserModel()
                    {
                        UserId = userId,
                        Password="Password123",
                        Status="Pending",
                        DateAdded= DateTime.UtcNow,
                        DateUpdated=DateTime.UtcNow,
                        LastLogin= DateTime.UtcNow,
                        Email=User.FindFirstValue(ClaimTypes.Email),
                        ProfilePicture= "c7a7ecc2-04c6-4c65-9734-b163c5a9ed47_69805e2b_20251101143926.jpg"
                    });
                    //return NotFound("User not found");

                // === 1. Load All Data ===
                var allSkills = await _dbSkillService.GetAllAsync();
                var allQualifications = await _dbQualificationService.GetAllAsync();
                var allTrainings = await _dbTrainingService.GetAllAsync();
                var allNotifications = await _notificationService.GetAllAsync();

                var userSkills = allSkills.Where(s => s.UserId == userId).ToList();
                var userQualifications = allQualifications.Where(q => q.UserId == userId).ToList();
                var userTrainings = allTrainings.Where(t => t.UserId == userId).ToList();

                // === 2. Pending Count ===
                var pendingCount = userSkills.Count(s => s.Status == "Pending") +
                                   userQualifications.Count(q => q.Status == "Pending") +
                                   userTrainings.Count(t => t.CompletionStatus== "Pending");

                // === 3. Upcoming Trainings (Next 30 days) ===
                var today = DateTime.UtcNow.Date;
                var next30 = today.AddDays(30);
                var upcomingTrainings = userTrainings
                    .Where(t => t.StartDate >= today && t.StartDate <= next30 && t.CompletionStatus != "Completed")
                    .Count();

                // === 4. Unread Notifications (from non-Employee) ===
                var unreadNotifications = allNotifications
                    .Where(n => n.UserId == userId && n.ReadStatus != "Read" && n.SentBy != "Employee")
                    .Take(5)
                    .ToList();

                // === 5. Recent Activities (Last 5 updates) ===
                List<ActivityItem> recentActivities = new List<ActivityItem>();

                foreach (var s in userSkills.OrderByDescending(s => s.DateUpdated).Take(3))
                {
                    recentActivities.Add(new ActivityItem
                    {
                        Message = $"Updated skill: <strong>{s.SkillName}</strong>",
                        Icon = "bi bi-lightning-charge-fill text-warning",
                        Link = $"/Skill/Details/{s.SkillId}",
                        TimeAgo = TimeAgo(s.DateUpdated),
                        IsRead = true
                    });
                }

                foreach (var q in userQualifications.OrderByDescending(q => q.DateUpdated).Take(3))
                {
                    recentActivities.Add(new ActivityItem
                    {
                        Message = $"Updated qualification: <strong>{q.QualificationName}</strong>",
                        Icon = "bi bi-mortarboard-fill text-success",
                        Link = $"/Qualification/Details/{q.QualificationId}",
                        TimeAgo = TimeAgo(q.DateUpdated),
                        IsRead = true
                    });
                }

                foreach (var t in userTrainings.OrderByDescending(t => t.StartDate).Take(3))
                {
                    recentActivities.Add(new ActivityItem
                    {
                        Message = $"Updated training: <strong>{t.TrainingName}</strong>",
                        Icon = "bi bi-journal-text text-info",
                        Link = $"/Training/Details/{t.TrainingId}",
                        TimeAgo = TimeAgo(t.StartDate),
                        IsRead = true
                    });
                }

                var sortedActivities = recentActivities
                    .Take(5)
                    .ToList();

                // === 6. Populate ViewBag (like your Profile) ===
                ViewBag.User = user;
                ViewBag.PendingCount = pendingCount;
                ViewBag.SkillsCount = userSkills.Count;
                ViewBag.UpcomingTrainings = upcomingTrainings;
                ViewBag.UnreadNotifications = unreadNotifications.Count;
                ViewBag.UnreadNotificationList = unreadNotifications;
                ViewBag.RecentActivities = sortedActivities;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Dashboard: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, "Internal server error");
            }
        }

        // Helper: Human-readable time ago
        private string TimeAgo(DateTime? date)
        {
            if (!date.HasValue) return "Just now";
            var ts = DateTime.UtcNow - date.Value.ToUniversalTime();
            return ts.TotalMinutes < 1 ? "Just now" :
                   ts.TotalMinutes < 60 ? $"{(int)ts.TotalMinutes}m ago" :
                   ts.TotalHours < 24 ? $"{(int)ts.TotalHours}h ago" :
                   $"{(int)ts.TotalDays}d ago";
        }
    

    // View Models (keep in same file or move to Models/)
    
        //public async Task<IActionResult> Dashboard()
        //{
        //    try
        //    {
        //        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        if (string.IsNullOrEmpty(userId))
        //            return RedirectToAction("Login", "Account");

        //        var user = await _dbService.GetByFieldAsync("userId", userId);
        //        if (user == null)
        //            return NotFound("User not found");
        //        var skills = (await _dbSkillService.GetAllAsync()).Where(s => s.UserId == userId).ToList();
        //        var qualifications = (await _dbQualificationService.GetAllAsync()).Where(q => q.UserId == userId).ToList();
        //        var trainings = (await _dbTrainingService.GetAllAsync()).Where(t => t.UserId == userId).ToList();
        //        var unreadNotifications = (await _notificationService.GetAllAsync())
        //            .Where(n => n.UserId == userId && n.ReadStatus != "Read").ToList();
        //        var filteredotifications = unreadNotifications.Where((n) => n.SentBy != "Employee").Select((n) => n);

        //        var recentActivities = new List<string>
        //    {
        //        "You updated your skill: Communication",
        //        "New recommended training: Leadership 101",
        //        "Qualification 'MBA' approved by HR"
        //    };
        //        ViewBag.User = user;
        //        ViewBag.Skills = skills;
        //        ViewBag.Qualifications = qualifications;
        //        ViewBag.Trainings = trainings;
        //        ViewBag.UnreadNotifications = filteredotifications;
        //        ViewBag.RecentActivities = recentActivities;

        //        ViewBag.Qualifications = qualifications;
        //        ViewBag.Skills = skills;

        //        return View(user);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error in Profile: {ex}");
        //        return StatusCode(500, "Internal server error");
        //    }
        //}



        [HttpGet]
        public async Task<IActionResult> LatestNotification()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _notificationService.GetAllAsync();

            var latest = notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.SentDate)
                .FirstOrDefault();

            return View(latest);
        }
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }
                
                var user = await _dbService.GetByFieldAsync("userId", userId);
                if (user == null)
                {
                    Console.WriteLine($"User not found for userId: {userId}");
                    return NotFound("User not found");
                }
                var qualifications = await _dbQualificationService.GetAllAsync();
                    var userQualifications = qualifications.Where(q => q.UserId == user.UserId).ToList();
                var skills = await _dbSkillService.GetAllAsync();
                var userSkills = skills.Where(q => q.UserId == user.UserId).ToList();
                var trainings = await _dbTrainingService.GetAllAsync();
                var userTraining = trainings.Where(q => q.UserId == user.UserId).ToList();

                ViewBag.Qualifications = userQualifications;
                ViewBag.Skills = userSkills;
                ViewBag.Trainings = userTraining;
                return View(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Profile action: {ex.Message}\nStack Trace: {ex.StackTrace}");
                return StatusCode(500, "An internal server error occurred. Please try again later.");
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var user = await _dbService.GetByFieldAsync("userId", userId);
            if (user == null) return View("NotFound");;

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserModel model, IFormFile? profileFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || model.UserId != userId)
                return Unauthorized();


            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Any())
                    .ToDictionary(x => x.Key, x => x.Value.Errors.Select(e => e.ErrorMessage).ToArray());
                Console.WriteLine("ModelState Errors: " + System.Text.Json.JsonSerializer.Serialize(errors));
                return View(model);
            }

            try
            {
                
                var existing = await _dbService.GetByFieldAsync("userId", userId);
                if (existing == null) return View("NotFound");;

                string newPictureFileName = existing.ProfilePicture; 

               
                if (profileFile != null && profileFile.Length > 0)
                {
                   
                    var allowed = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                    if (!allowed.Contains(profileFile.ContentType.ToLowerInvariant()))
                    {
                        ModelState.AddModelError("profileFile", "Only JPG, PNG, GIF, WebP allowed.");
                        return View(model);
                    }

                   
                    var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    Directory.CreateDirectory(folder);

                    
                    var guid8 = Guid.NewGuid().ToString("N")[..8];
                    var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                    var ext = Path.GetExtension(profileFile.FileName).ToLowerInvariant();
                    var fileName = $"{userId}_{guid8}_{timestamp}{ext}";
                    var fullPath = Path.Combine(folder, fileName);

                    await using (var stream = new FileStream(fullPath, FileMode.Create))
                        await profileFile.CopyToAsync(stream);

                    
                    if (!string.IsNullOrWhiteSpace(existing.ProfilePicture))
                    {
                        var oldPath = Path.Combine(folder, existing.ProfilePicture);
                        if (System.IO.File.Exists(oldPath) && !oldPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
                            System.IO.File.Delete(oldPath);
                    }

                    newPictureFileName = fileName; 
                }

                existing.FullName = model.FullName?.Trim();
                existing.Email = model.Email?.Trim();
                existing.Position = model.Position?.Trim();
                existing.ProfilePicture = newPictureFileName;

                
                existing.DateUpdated = DateTime.UtcNow; 
                if (existing.DateAdded.Kind != DateTimeKind.Utc)
                    existing.DateAdded = existing.DateAdded.ToUniversalTime();
                if (existing.LastLogin != default && existing.LastLogin.Kind != DateTimeKind.Utc)
                    existing.LastLogin = existing.LastLogin.ToUniversalTime();

                await _dbService.UpdateAsync(userId, existing);

                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Edit POST] Firestore error: {ex}");
                ModelState.AddModelError("", "Failed to save profile. Check server logs.");
                return View(model);
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || file == null)
                return Json(new { success = false, message = "Invalid upload" });

            try
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var fileName = $"{userId}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                
                var user = await _dbService.GetByFieldAsync("userId", userId);
                user.ProfilePicture = $"/images/{fileName}";
                await _dbService.UpdateAsync(userId, user);

                return Json(new { success = true, newPath = user.ProfilePicture });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
