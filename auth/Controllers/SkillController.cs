using auth.Models.Models;
using Auth.Models.Models;
using Auth.Models.ViewModels;
using Auth.Services;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auth.Controllers
{
    public class SkillController : Controller
    {
        private readonly DbServices<SkillModel> _dbService;
        //private readonly SessionHelper _sessionHelper;
        private readonly DbServices<UserModel> _userService;
        private readonly DbServices<NotificationModel> _dbNotificatoionService;
        public SkillController()
        {
            _userService = new DbServices<UserModel>("users");
            _dbService = new DbServices<SkillModel>("skills");
            _dbNotificatoionService = new DbServices<NotificationModel>("notifications");
            //_sessionHelper = new SessionHelper(accessor);
        }

        private readonly Dictionary<string, string[]> CategoryKeywords = new()
        {
            ["IT"] = new[] { "c#", "asp.net", "aspnet", "dotnet", "java", "javascript", "python", "azure", "aws", "docker", "kubernetes", "react", "node", "sql", "database", "devops", "cloud" },
            ["Finance"] = new[] { "finance", "financial", "excel", "power bi", "accounting", "ifrs", "forecast", "modelling", "valuation", "audit", "tax", "bookkeeping" },
            ["Operation"] = new[] { "lean", "six sigma", "supply chain", "logistics", "process", "kpi", "operations", "project management", "pm" }
        };
        public async Task<IActionResult> List()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            var skills = await _dbService.GetAllAsync();
            var userSkills = skills.FindAll(s => s.UserId == userId);
            return View(userSkills);
        }

        [HttpGet]
        public IActionResult Add()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            var model = new SkillModel
            {
                SkillId = Guid.NewGuid().ToString(),
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                DateAdded = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                ApprovedBy = "CODE117",
                Status = "Pending",
                DateUpdated = DateTime.UtcNow
            }; 

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            var skill = await _dbService.GetByIdAsync(id);
            return View(skill);
        }

        [HttpPost]
        public async Task<IActionResult> Add(SkillModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            if (ModelState.IsValid)
            {
                model.SkillId = Guid.NewGuid().ToString();
                model.DateAdded = DateTime.UtcNow;
                model.DateUpdated = DateTime.UtcNow;
                var user = await _userService.GetByFieldAsync("userId", userId);
                //string helperText = nameof(SkillModel);

                var notification = new NotificationModel
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    Message = $"A new Skill was added by {user?.FullName ?? "User"} on {model.DateAdded:MMMM dd, yyyy}.",
                    ReadStatus = "Unread",
                    SentBy = user?.Role ?? "Employee",
                    SentDate = DateTime.UtcNow,
                    UserId = userId
                };

                await _dbNotificatoionService.CreateAsync(notification.NotificationId, notification);
                TempData["SuccessMessage"] = "Skill added successfully!";
                await _dbService.CreateAsync(model.SkillId, model);
                return RedirectToAction("List");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            var skill = await _dbService.GetByIdAsync(id);
            if (skill == null)
                return NotFound();

            return View(skill);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SkillModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid)
                return View(model);
            model.DateUpdated =DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            model.DateAdded =DateTime.SpecifyKind(model.DateAdded, DateTimeKind.Utc);
            var user = await _userService.GetByFieldAsync("userId", userId);

            //string helperText = nameof(SkillModel);
            var notification = new NotificationModel
            {
                NotificationId = Guid.NewGuid().ToString(),
                Message = $"A Skill with UID: ${model.SkillId} was edited on the ${model.DateAdded}.",
                ReadStatus = "Unread",
                SentBy = user?.Role ?? "Employee",
                SentDate = DateTime.UtcNow,
                UserId = userId
            };

            await _dbNotificatoionService.CreateAsync(notification.NotificationId, notification);
            await _dbService.UpdateAsync(model.SkillId, model);
            TempData["SuccessMessage"] = "Skill updated successfully.";

            return RedirectToAction("List");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            var skill = await _dbService.GetByIdAsync(id);
            if (skill == null)
                return NotFound();

            return View(skill);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(SkillModel skill)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            if (string.IsNullOrEmpty(skill.SkillId))
                return BadRequest("Invalid Skill ID.");

            await _dbService.DeleteAsync(skill.SkillId);
            TempData["SuccessMessage"] = "Skill removed successfully.";

            return RedirectToAction("List");
        }

        private string GuessCategory(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName)) return "Other";
            var lower = skillName.ToLowerInvariant();

            foreach (var kv in CategoryKeywords)
            {
                foreach (var kw in kv.Value)
                {
                    if (lower.Contains(kw))
                        return kv.Key;
                }
            }

            return "Other";
        }

        public async Task<IActionResult> GapAnalysis()
        {
            var allSkills = (await _dbService.GetAllAsync())?.ToList() ?? new List<SkillModel>();
            var allUsers = (await _userService.GetAllAsync())?.ToList() ?? new List<UserModel>();

            var employees = allUsers.Where(user => string.Equals(user.Role, "Employee", System.StringComparison.OrdinalIgnoreCase));

            var gapAnalysis = employees.Select(u =>
            {
                var missing = allSkills
                    .Where(s => s.UserId == u.UserId && string.IsNullOrEmpty(s.Status))
                    .ToList();

                return new SkillGapViewModel
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    MissingSkills = missing
                };
            }).ToList();

            ViewBag.LoggedInName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //return Content($"{gapAnalysis.Count}");
            return View(gapAnalysis);
        }
        public async Task<IActionResult> ProficiencyCharts()
        {
            var allSkills = (await _dbService.GetAllAsync())?.ToList() ?? new List<SkillModel>();

            double SkillLevelToNumber(string level) => level?.ToLower() switch
            {
                "beginner" => 1,
                "intermediate" => 2,
                "advanced" => 3,
                _ => 0
            };

            var chartData = allSkills
                .GroupBy(s => s.SkillName)
                .Select(g => new SkillProficiencyViewModel
                {
                    SkillName = g.Key,
                    AverageLevel = g.Average(s => SkillLevelToNumber(s.SkillLevel))
                })
                .OrderByDescending(x => x.AverageLevel)
                .ToList();

            ViewBag.LoggedInName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return View(chartData);
        }

        public async Task<IActionResult> Trending()
        {
            var allSkills = (await _dbService.GetAllAsync())?.ToList() ?? new List<SkillModel>();

            var categories = new Dictionary<string, List<string>>
            {
                ["IT"] = new List<string>(),
                ["Finance"] = new List<string>(),
                ["Operation"] = new List<string>(),
                ["Other"] = new List<string>()
            };

            foreach (var s in allSkills)
            {
                var catRaw = (s.GetType().GetProperty("Category")?.GetValue(s)?.ToString() ?? "").Trim();
                var cat = string.IsNullOrWhiteSpace(catRaw) ? GuessCategory(s.SkillName) : catRaw;

                if (cat.Equals("it", StringComparison.OrdinalIgnoreCase)) cat = "IT";
                if (cat.Equals("operations", StringComparison.OrdinalIgnoreCase)) cat = "Operation";
                if (cat.Equals("finance", StringComparison.OrdinalIgnoreCase)) cat = "Finance";

                if (!categories.ContainsKey(cat)) categories[cat] = new List<string>();
                if (!string.IsNullOrWhiteSpace(s.SkillName) && !categories[cat].Contains(s.SkillName))
                    categories[cat].Add(s.SkillName);
            }

            var topOverall = allSkills
                .GroupBy(x => x.SkillName)
                .Select(g => new { Name = g.Key, Count = g.Select(x => x.UserId).Distinct().Count() })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Name)
                .Select(x => x.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            foreach (var key in categories.Keys.ToList())
            {
                if (categories[key] == null) categories[key] = new List<string>();
                if (!categories[key].Any())
                {
                    foreach (var t in topOverall)
                    {
                        if (categories[key].Count >= 5) break;
                        if (!categories.Values.SelectMany(v => v).Contains(t))
                        {
                            categories[key].Add(t);
                        }
                    }
                }
            }

            var ordered = new Dictionary<string, List<string>>
            {
                ["IT"] = categories.ContainsKey("IT") ? categories["IT"] : new List<string>(),
                ["Finance"] = categories.ContainsKey("Finance") ? categories["Finance"] : new List<string>(),
                ["Operation"] = categories.ContainsKey("Operation") ? categories["Operation"] : new List<string>(),
                ["Other"] = categories.ContainsKey("Other") ? categories["Other"] : new List<string>()
            };

            var vm = new TrendingSkillsViewModel { Categories = ordered };

            ViewBag.LoggedInName = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return View(vm);
        }

    }
}
