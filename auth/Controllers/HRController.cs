using auth.Models.Models;
using Auth.Models.Models;
using Auth.Models.NewFolder;
using Auth.Models.ViewModels;
using Auth.Services;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auth.Controllers
{
    public class HRController : Controller
    {
        private readonly DbServices<UserModel> _userService;
        private readonly DbServices<AuditModel> _auditService;
        private readonly DbServices<SkillModel> _skillService;
        private readonly DbServices<QualificationModel> _qualificationService;
        private readonly DbServices<TrainingModel> _trainingService;
        private readonly DbServices<NotificationModel> _notificationService;
        private readonly DbServices<ReportModel> _reportService;
        private readonly DbServices<object> _recommendedTrainingService;

        private readonly StorageService _storageService;
        public HRController()
        {
            _userService = new DbServices<UserModel>("users");
            _auditService = new DbServices<AuditModel>("audits");
            _skillService = new DbServices<SkillModel>("skills");
            _qualificationService = new DbServices<QualificationModel>("qualifications");
            _trainingService = new DbServices<TrainingModel>("trainings");
            _notificationService = new DbServices<NotificationModel>("notifications");
            _reportService = new DbServices<ReportModel>("reports");
            _storageService = new StorageService();
            _recommendedTrainingService = new DbServices<object>("recommendedTrainings");
        }
        /// <summary>
        /// /////////////////////////


        // ==============================
        // DOWNLOAD QUALIFICATION DOCUMENT
        // ==============================
        [HttpGet]
        public async Task<IActionResult> DownloadQualificationDocument(string userId, string qualificationId, string fileName)
        {
            try
            {
                string folderPath = $"qualifications/{userId}";
                var storage = new Firebase.Storage.FirebaseStorage("streamline-4994a.firebasestorage.app");

                var downloadUrl = await storage
                    .Child(folderPath)
                    .Child(fileName)
                    .GetDownloadUrlAsync();

                // Redirect user to the public Firebase download URL
                return Redirect(downloadUrl);
            }
            catch (Exception ex)
            {
                // You can log ex.Message
                TempData["Error"] = "Error downloading qualification document.";
                return RedirectToAction("UserDetails", new { userId });
            }
        }

        [HttpGet]
        public IActionResult DownloadCertificate(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return NotFound("Document URL not found.");

            // Ensure the URL is a Firebase Storage URL
            if (!fileUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid file URL.");

            // Redirects the user to Firebase’s public link
            return Redirect(fileUrl);
        }


        // ==========================
        // DOWNLOAD TRAINING DOCUMENT
        // ==========================
        [HttpGet]
        public async Task<IActionResult> DownloadTrainingDocument(string userId, string trainingId, string fileName)
        {
            try
            {
                string folderPath = $"trainings/{userId}";
                var storage = new Firebase.Storage.FirebaseStorage("streamline-4994a.firebasestorage.app");

                var downloadUrl = await storage
                    .Child(folderPath)
                    .Child(fileName)
                    .GetDownloadUrlAsync();

                return Redirect(downloadUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error downloading training document.";
                return RedirectToAction("UserDetails", new { userId });
            }
        }

        /// </summary>
        /// <returns></returns>
        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public async Task<IActionResult> Dashboard()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            ViewBag.LoggedInName = userId;
            ViewBag.LoggedInUserName = userId;
            var user = await _userService.GetByFieldAsync("userId", userId);
            return View(user);
        }

        public async Task<IActionResult> Trainings()
        {
            //ViewBag.LoggedInName = GetCurrentUserId();
            var trainings = await _trainingService.GetAllAsync();
            return View(trainings);
        }
        [HttpGet]
        public IActionResult DownloadDocument(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return NotFound();

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "training_docs");
            var filePath = Path.Combine(folder, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var contentType = "application/octet-stream";
            return PhysicalFile(filePath, contentType, fileName);
        }
        public async Task<IActionResult> TrainingDetails(string id)
        {
            //return Content(id);
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Trainings");

            ViewBag.LoggedInName = GetCurrentUserId();

            var training = await _trainingService.GetByIdAsync(id);
            if (training == null)
                return RedirectToAction("Trainings");

            var allUsers = await _userService.GetAllAsync();
            var allTrainings = await _trainingService.GetAllAsync();

            var relatedTrainings = allTrainings.Where(t => t.TrainingId == id).ToList();

            var usersInTraining = (from user in allUsers
                                   join t in relatedTrainings on user.UserId equals t.UserId
                                   where string.Equals(t.CompletionStatus, training.CompletionStatus, StringComparison.OrdinalIgnoreCase)
                                   select user).ToList();

            ViewBag.Training = training;training.DocumentUrl = training.DocumentUrl;
            ViewBag.UsersInTraining = usersInTraining;

            return View(training);
        }

        public async Task<IActionResult> Users()
        {
            ViewBag.LoggedInName = GetCurrentUserId();

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
            ViewBag.LoggedInName = GetCurrentUserId();
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

        private async Task LogAuditAsync(string userId, string action, string details)
        {
            var audit = new AuditModel
            {
                AuditId = Guid.NewGuid().ToString(),
                UserId = userId,
                Action = action,
                Details = details,
                Date = DateTime.UtcNow,
                AuditedBy = GetCurrentUserId()
            };
            await _auditService.CreateAsync(audit.AuditId, audit);
        }

        public async Task<IActionResult> Audits()
        {
            ViewBag.LoggedInName = GetCurrentUserId();
            var audits = await _auditService.GetAllAsync();
            var auditList = audits.Select(a => new AuditViewModel
            {
                AuditId = a.AuditId,
                UserId = a.UserId,
                Date = a.Date,
                AuditedBy = GetCurrentUserId(),
                Action=a.Action
                
            }).OrderByDescending(a => a.Date).ToList();

            return View(auditList);
        }

        public async Task<IActionResult> AuditDetails(string auditId)
        {
            ViewBag.LoggedInName = GetCurrentUserId();
            if (string.IsNullOrEmpty(auditId))
                return NotFound();

            var audit = await _auditService.GetByIdAsync(auditId);
            if (audit == null)
                return NotFound();
            //return Content(audit.Action);
            var viewModel = new AuditModel
            {
                AuditId = audit.AuditId,
                UserId = audit.UserId,
                Date = audit.Date,
                AuditedBy = audit.AuditedBy,
                Action=audit.Action
            };

            var user = await _userService.GetByIdAsync(audit.UserId);
            ViewBag.UserFullName = user?.FullName ?? "Unknown";

            return View(audit);
        }

        public async Task<IActionResult> Reports()
        {
            ViewBag.LoggedInName = GetCurrentUserId();
            var reports = await _reportService.GetAllAsync();
            var reportList = reports.Select(a => new ReportModel
            {
                ReportId = a.ReportId,
                UserId = a.UserId,
                Title = a.Title,
                //GeneratedDate = a.
            }).ToList();

            return View(reportList);
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

            var filteredotifications = notifications.Where((n) => n.SentBy == "Employee" || n.SentBy == "Admin").Select((n) => n);
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
        public async Task<IActionResult> Profile()
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToAction("Login", "Account");

            var user = await _userService.GetByIdAsync(currentUserId); // use session userId
            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userService.GetByFieldAsync("userId",userId);
            //return Content(user!.FullName);
            if (user == null)
                return NotFound();
            user.Role = "Deactivated";
            await _userService.UpdateAsync(userId,user);
            var userInfor = await _userService.GetByFieldAsync("userId", User.FindFirstValue(ClaimTypes.NameIdentifier));
            AuditModel newAudit = new()
            {
                AuditId = Guid.NewGuid().ToString(),
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                AuditedBy = userInfor!.FullName,
                Date = DateTime.UtcNow,

                Action = "User removal",
                Details = $"User \"{user.FullName}'s\" account was deactivated by {userInfor.FullName} on {DateTime.UtcNow:dd MMM yyyy HH:mm} (UTC)."
            };


            await _auditService.CreateAsync(newAudit.AuditId, newAudit);
            //await _userService.(userId);
            //var userInfor = await _userService.GetByFieldAsync("userId", GetCurrentUserId);
            //AuditModel newAudit = new AuditModel
            //{
            //    Action= "remove user",
            //    AuditedBy= userInfor!.FullName,
            //    AuditId= Guid.NewGuid().ToString(),
            //    Date=DateTime.UtcNow,
            //    Details=$"Removed user with {userId} on ${DateTime.UtcNow}",
            //    UserId=GetCurrentUserId()
            //};

            //await _auditService.CreateAsync(newAudit.AuditId, newAudit);
            //await LogAuditAsync(userId, "Deleted User", $"User {user.FullName} was deleted.");

            TempData["Success"] = $"User {user.FullName} was deleted successfully.";
            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> ApproveSkill(string skillId)
        {
            //return Content(skillId);
            var skill = await _skillService.GetByFieldAsync("skillId", skillId);
            if (skill == null)
                return NotFound();

            skill.Status = "Approved";
            await _skillService.UpdateAsync(skillId, skill);

            //await LogAuditAsync(skill.UserId, "Approved Skill", $"Skill: {skill.SkillName}, Level: {skill.SkillLevel}");
            var userInfor = await _userService.GetByFieldAsync("userId", User.FindFirstValue(ClaimTypes.NameIdentifier));
            AuditModel newAudit = new()
            {
                AuditId = Guid.NewGuid().ToString(),
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                AuditedBy = userInfor!.FullName,
                Date = DateTime.UtcNow,

                Action = "Skill Approval",
                Details = $"Skill \"{skill.SkillName}\" was approved by {userInfor.FullName} on {DateTime.UtcNow:dd MMM yyyy HH:mm} (UTC)."
            };


            await _auditService.CreateAsync(newAudit.AuditId, newAudit);
            var user = await _userService.GetByFieldAsync("userId", User.FindFirstValue(ClaimTypes.NameIdentifier));
            //string helperText = nameof(SkillModel);

            var notification = new NotificationModel
            {
                NotificationId = Guid.NewGuid().ToString(),
                Message = $"Your skill has been approved by Hr",
                ReadStatus = "Unread",
                SentBy = user?.Role ?? "Hr",
                SentDate = DateTime.UtcNow,
                UserId = skill.UserId
            };

            await _notificationService.CreateAsync(notification.NotificationId, notification);
            TempData["Success"] = "Skill approved and audit logged.";
            TempData["ShowRecommendModal"] = skill.SkillId;
            TempData["RecommendedSkillName"] = skill.SkillName;
            return RedirectToAction("UserDetails", new { id = skill.UserId });

            //return RedirectToAction("UserDetails", new { id = skill.UserId });
        }

        [HttpPost]
        public async Task<IActionResult> RejectSkill(string skillId, string reason)
        {
            var skill = await _skillService.GetByIdAsync(skillId);
            if (skill == null)
                return NotFound();

            skill.Status = "Rejected";
            await _skillService.UpdateAsync(skillId, skill);
            var userInfor = await _userService.GetByFieldAsync("userId", User.FindFirstValue(ClaimTypes.NameIdentifier));
            AuditModel newAudit = new()
            {
                AuditId = Guid.NewGuid().ToString(),
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                AuditedBy = userInfor!.FullName,
                Date = DateTime.UtcNow,

                Action = "Skill rejected",
                Details = $"Skill \"{skill.SkillName}\" was rejected by {userInfor.FullName} on {DateTime.UtcNow:dd MMM yyyy HH:mm} (UTC)."
            };
            await _auditService.CreateAsync(newAudit.AuditId, newAudit);
            var user = await _userService.GetByFieldAsync("userId", User.FindFirstValue(ClaimTypes.NameIdentifier));
            //string helperText = nameof(SkillModel);

            var notification = new NotificationModel
            {
                NotificationId = Guid.NewGuid().ToString(),
                Message = $"Your skill has been rejected by Hr",
                ReadStatus = "Unread",
                SentBy = user?.Role ?? "Hr",
                SentDate = DateTime.UtcNow,
                UserId = skill.UserId
            };

            await _notificationService.CreateAsync(notification.NotificationId, notification);
            await LogAuditAsync(skill.UserId, "Rejected Skill", $"Skill: {skill.SkillName}, Reason: {reason}");
            TempData["Error"] = "Skill rejected and audit logged.";

            return RedirectToAction("UserDetails", new { id = skill.UserId });
        }

        [HttpPost]
        public async Task<IActionResult> ApproveQualification(string qualificationId)
        {
            var qualification = await _qualificationService.GetByIdAsync(qualificationId);
            if (qualification == null)
                return NotFound();

            qualification.Status = "Approved";
            await _qualificationService.UpdateAsync(qualificationId, qualification);
            var userInfor = await _userService.GetByFieldAsync("userId", User.FindFirstValue(ClaimTypes.NameIdentifier));
            AuditModel newAudit = new()
            {
                AuditId = Guid.NewGuid().ToString(),
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                AuditedBy = userInfor!.FullName,
                Date = DateTime.UtcNow,

                Action = "Qualification Approval",
                Details = $"Qualification \"{qualification.QualificationName}\" was approved by {userInfor.FullName} on {DateTime.UtcNow:dd MMM yyyy HH:mm} (UTC)."
            };
            var user = await _userService.GetByFieldAsync("userId", User.FindFirstValue(ClaimTypes.NameIdentifier));
            //string helperText = nameof(SkillModel);

            var notification = new NotificationModel
            {
                NotificationId = Guid.NewGuid().ToString(),
                Message = $"Your qualification has been approved by Hr",
                ReadStatus = "Unread",
                SentBy = user?.Role ?? "Hr",
                SentDate = DateTime.UtcNow,
                UserId = qualification.UserId
            };

            await _notificationService.CreateAsync(notification.NotificationId, notification);

            await _auditService.CreateAsync(newAudit.AuditId, newAudit);
            await LogAuditAsync(qualification.UserId, "Approved Qualification",
                $"Qualification: {qualification.QualificationName}, Institution: {qualification.InstitutionName}");

            TempData["Success"] = "Qualification approved and audit recorded.";
            return RedirectToAction("UserDetails", new { id = qualification.UserId });
        }

        [HttpPost]
        public async Task<IActionResult> RejectQualification(string qualificationId, string reason)
        {
            var qualification = await _qualificationService.GetByIdAsync(qualificationId);
            if (qualification == null)
                return NotFound();

            qualification.Status = "Rejected";
            await _qualificationService.UpdateAsync(qualificationId, qualification);
            var user = await _userService.GetByFieldAsync("userId", User.FindFirstValue(ClaimTypes.NameIdentifier));
            //string helperText = nameof(SkillModel);

            var notification = new NotificationModel
            {
                NotificationId = Guid.NewGuid().ToString(),
                Message = $"Your qualification has been REJECTED by Hr",
                ReadStatus = "Unread",
                SentBy = user?.Role ?? "Hr",
                SentDate = DateTime.UtcNow,
                UserId = qualification.UserId
            };

            await _notificationService.CreateAsync(notification.NotificationId, notification);
            //var userInfor = await _userService.GetByFieldAsync("userId", User.FindFirstValue(ClaimTypes.NameIdentifier));
            //AuditModel newAudit = new AuditModel
            //{
            //    AuditId = Guid.NewGuid().ToString(),
            //    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            //    AuditedBy = userInfor!.FullName,
            //    Date = DateTime.UtcNow,

            //    Action = "Qualification rejected",
            //    Details = $"Qualification \"{qualification.QualificationName}\" was approved by {userInfor.FullName} on {DateTime.UtcNow:dd MMM yyyy HH:mm} (UTC)."
            //};


            //await _auditService.CreateAsync(newAudit.AuditId, newAudit);
            TempData["Error"] = "Qualification rejected and audit logged.";

            await LogAuditAsync(qualification.UserId, "Rejected Qualification",
                $"Qualification: {qualification.QualificationName}, Institution: {qualification.InstitutionName}, Reason: {reason}");
            return RedirectToAction("UserDetails", new { id = qualification.UserId });
        }

        [HttpPost]
        public async Task<IActionResult> RecommendTraining(string userId, string trainingName)
        {
            var recommendation = new
            {
                UserId = userId,
                TrainingName = trainingName,
                RecommendedBy = GetCurrentUserId(),
                DateRecommended = Timestamp.GetCurrentTimestamp()
            };

            await _recommendedTrainingService.CreateAsync(Guid.NewGuid().ToString(), recommendation);
            TempData["Success"] = $"Training '{trainingName}' recommended.";
            return RedirectToAction("UserDetails", new { id = userId });
        }


        //Makgowa
        private readonly Dictionary<string, int> SkillLevelMapping = new()
        {
            { "Beginner", 1 },
            { "Intermediate", 2 },
            { "Advanced", 3 }
        };

        private readonly Dictionary<string, string[]> CategoryKeywords = new()
        {
            ["IT"] = new[] { "c#", "asp.net", "aspnet", "dotnet", "java", "javascript", "python", "azure", "aws", "docker", "kubernetes", "react", "node", "sql", "database", "devops", "cloud" },
            ["Finance"] = new[] { "finance", "financial", "excel", "power bi", "accounting", "ifrs", "forecast", "modelling", "valuation", "audit", "tax", "bookkeeping" },
            ["Operation"] = new[] { "lean", "six sigma", "supply chain", "logistics", "process", "kpi", "operations", "project management", "pm" }
        };
        [HttpGet]
        public async Task<IActionResult> RefreshUnread()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var allNotifications = await _notificationService.GetAllAsync();

            return PartialView("_UnreadNotificationsPartial", allNotifications);
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
            var allSkills = (await _skillService.GetAllAsync())?.ToList() ?? new List<SkillModel>();
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
            var allSkills = (await _skillService.GetAllAsync())?.ToList() ?? new List<SkillModel>();

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
            var allSkills = (await _skillService.GetAllAsync())?.ToList() ?? new List<SkillModel>();

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