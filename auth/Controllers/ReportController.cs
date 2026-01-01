using auth.Models.Models;
using Auth.Models.Models;
using Auth.Services;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Route("reports")]
public class ReportController : Controller
{

    private readonly DbServices<UserModel> _userService;
    private readonly DbServices<ReportModel> _dbReportService;
    private readonly DbServices<NotificationModel> _dbNotificatoionService;
    public ReportController() {
    
        _userService = new DbServices<UserModel>("users");
        _dbReportService = new DbServices<ReportModel>("reports");
        _dbNotificatoionService = new DbServices<NotificationModel>("notifications");
    }

    // GET: /reports
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (string.IsNullOrEmpty(User.FindFirstValue(ClaimTypes.NameIdentifier)))
            return Unauthorized();

        var reports = await _dbReportService.GetAllAsync();
        var userReports = reports.FindAll(s => s.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier));
        return View(userReports);
    }

    // GET: /reports/submit
    [HttpGet("submit")]
    public IActionResult Submit() => View(new ReportModel());

    // POST: /reports/submit
    [HttpPost("submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ReportModel model)
    {
        model.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        model.ReportId = Guid.NewGuid().ToString(); // Let Firestore generate
        model.SubmittedAt = DateTime.UtcNow;
        model.Status = "Submitted";
        var user = await _userService.GetByFieldAsync("userId", User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (ModelState.IsValid)
        {
            //string helperText = nameof(SkillModel);

            var notification = new NotificationModel
            {
                NotificationId = Guid.NewGuid().ToString(),
                Message = $"A new Report was added by {user?.FullName ?? "User"} on {model.SubmittedAt:MMMM dd, yyyy}.",
                ReadStatus = "Unread",
                SentBy = user?.Role ?? "Employee",
                SentDate = DateTime.UtcNow,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            TempData["SuccessMessage"] = "Skill added successfully!";
            await _dbNotificatoionService.CreateAsync(notification.NotificationId, notification);
            await _dbReportService.CreateAsync(model.ReportId, model);
            return RedirectToAction("List");
        }

        return View(model);
        //TempData["SuccessMessage"] = "Report submitted successfully!";
        //return RedirectToAction("Index");
    }
}