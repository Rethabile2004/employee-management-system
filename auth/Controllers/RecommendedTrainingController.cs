using auth.Models.Models;
using Auth.Models.Models;
using Auth.Services;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Streamline.Controllers
{
    public class RecommendedTrainingController : Controller
    {
        private readonly DbServices<RecommendedTrainingModel> _dbService;
        //private readonly SessionHelper _sessionHelper;
        private readonly DbServices<UserModel> _userService;
        private readonly DbServices<NotificationModel> _dbNotificatoionService;
        public RecommendedTrainingController(IHttpContextAccessor accessor)
        {
        _userService = new DbServices<UserModel>("users");
        _dbNotificatoionService = new DbServices<NotificationModel>("notifications");
            _dbService = new DbServices<RecommendedTrainingModel>("recommendedTrainings");
            //_sessionHelper = new SessionHelper(accessor);
        }


        public async Task<IActionResult> List()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var all = await _dbService.GetAllAsync();
            var userTrainings = all.FindAll(t => t.UserId == userId);
            return View(userTrainings);
        }
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            var training = await _dbService.GetByIdAsync(id);
            return View(training);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTraining(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var training = await _dbService.GetByIdAsync(id);
            if (training == null || training.UserId != userId || training.Status != "Pending")
                return NotFound();

            training.DateApproved = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            training.Status = "Started";

            await _dbService.UpdateAsync(id, training);

            TempData["SuccessMessage"] = "Training started successfully!";
            return RedirectToAction("Details", new { id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleComplete(string id, IFormFile? certificateFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var training = await _dbService.GetByIdAsync(id);
            if (training == null || training.UserId != userId || training.Status == "Pending")
                return NotFound();


            string newFileName = training.Document;
            if (certificateFile != null && certificateFile.Length > 0)
            {
                var allowed = new[] { "application/pdf", "image/jpeg", "image/jpg", "image/png" };
                if (!allowed.Contains(certificateFile.ContentType.ToLowerInvariant()))
                {
                    ModelState.AddModelError("certificateFile", "Only PDF, JPG, and PNG files are allowed.");
                    return RedirectToAction("Details", new { id });
                }

                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "certificates");
                Directory.CreateDirectory(folder);

                var guid8 = Guid.NewGuid().ToString("N")[..8];
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var ext = Path.GetExtension(certificateFile.FileName).ToLowerInvariant();
                var fileName = $"{userId}_{guid8}_{timestamp}{ext}";
                var fullPath = Path.Combine(folder, fileName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                    await certificateFile.CopyToAsync(stream);

                newFileName = fileName;
            }


            var user = await _userService.GetByFieldAsync("userId", userId);
            var newNotification = new NotificationModel
            {
                NotificationId = Guid.NewGuid().ToString(),
                Message = $"{user!.FullName} has completed the recommended training '{training.TrainingName}'. HR approval is required.",
                ReadStatus = "Unread",
                SentBy = user.Role,
                SentDate = DateTime.UtcNow,
                UserId = user.UserId
            };
            await _dbNotificatoionService.CreateAsync(newNotification.NotificationId, newNotification);


            training.Document = newFileName;
            training.DateCompleted = DateTime.UtcNow;
            training.Status = "awaiting approval";

            await _dbService.UpdateAsync(id, training);

            TempData["SuccessMessage"] = "Certificate uploaded and completion submitted for approval.";
            return RedirectToAction("Details", new { id });
        }


    //    [HttpPost]
    //    [ValidateAntiForgeryToken]
    //    public async Task<IActionResult> MarkCompleted(string id)
    //    {
    //        var training = await _dbService.GetByIdAsync(id);
    //        if (training == null || training.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier) || training.Status != "Accepted")
    //            return Json(new { success = false });

    //        var updates = new Dictionary<FieldPath, object>
    //         {
    //     { new FieldPath("status"), "Pending approval" },
    //    { new FieldPath("dateCompleted"), FieldValue.ServerTimestamp }
    //};

    //        await _dbService.UpdateFieldsAsync(id, updates);
    //        return Json(new { success = true, message = "Completion submitted for HR review." });
    //    }
    }
}