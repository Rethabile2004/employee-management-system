using auth.Models.Models;
using Auth.Models.Models;
using Auth.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Streamline.Controllers
{
    public class ActivityController : Controller
    {
        private readonly DbServices<SkillModel> _dbSkillService;
        private readonly DbServices<TrainingModel> _dbTrainingService;
        private readonly DbServices<QualificationModel> _dbQualificationService;

        public ActivityController()
        {
            _dbSkillService = new DbServices<SkillModel>("skills");
            _dbTrainingService = new DbServices<TrainingModel>("trainings");
            _dbQualificationService = new DbServices<QualificationModel>("qualifications");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                // Load all data
                var allSkills = await _dbSkillService.GetAllAsync();
                var allQualifications = await _dbQualificationService.GetAllAsync();
                var allTrainings = await _dbTrainingService.GetAllAsync();

                var userSkills = allSkills.Where(s => s.UserId == userId).ToList();
                var userQualifications = allQualifications.Where(q => q.UserId == userId).ToList();
                var userTrainings = allTrainings.Where(t => t.UserId == userId).ToList();

                // Build activity log
                var activities = new List<ActivityLogItem>();

                foreach (var s in userSkills)
                {
                    activities.Add(new ActivityLogItem
                    {
                        Message = $"Skill: <strong>{s.SkillName}</strong> was {(s.Status == "Pending" ? "submitted" : s.Status.ToLower())}",
                        Icon = "bi bi-lightning-charge-fill text-warning",
                        Link = $"/Skill/Details/{s.SkillId}",
                        Timestamp = s.DateUpdated,
                        Type = "Skill"
                    });
                }

                foreach (var q in userQualifications)
                {
                    activities.Add(new ActivityLogItem
                    {
                        Message = $"Qualification: <strong>{q.QualificationName}</strong> was {(q.Status == "Pending" ? "submitted" : q.Status.ToLower())}",
                        Icon = "bi bi-mortarboard-fill text-success",
                        Link = $"/Qualification/Details/{q.QualificationId}",
                        Timestamp = q.DateUpdated,
                        Type = "Qualification"
                    });
                }

                foreach (var t in userTrainings)
                {
                    activities.Add(new ActivityLogItem
                    {
                        Message = $"Training: <strong>{t.TrainingName}</strong> was {(t.CompletionStatus == "Pending" ? "submitted" : t.CompletionStatus.ToLower())}",
                        Icon = "bi bi-journal-text text-info",
                        Link = $"/Training/Details/{t.TrainingId}",
                        Timestamp = t.StartDate,
                        Type = "Training"
                    });
                }

                var sortedActivities = activities
                    .OrderByDescending(a => a.Timestamp)
                    .ToList();

                ViewBag.Activities = sortedActivities;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Activity/Index: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class ActivityLogItem
    {
        public string Message { get; set; } = "";
        public string Icon { get; set; } = "bi bi-activity";
        public string Link { get; set; } = "#";
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = "";
        public string TimeAgo => TimeAgo1(Timestamp);

        private string TimeAgo1(DateTime date)
        {
            var ts = DateTime.UtcNow - date.ToUniversalTime();
            return ts.TotalMinutes < 1 ? "Just now" :
                   ts.TotalMinutes < 60 ? $"{(int)ts.TotalMinutes}m ago" :
                   ts.TotalHours < 24 ? $"{(int)ts.TotalHours}h ago" :
                   ts.Days < 7 ? $"{ts.Days}d ago" :
                   date.ToString("MMM dd, yyyy");
        }
    }
}