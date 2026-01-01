using auth.Models.Models;
using Auth.Models.Models;
using Auth.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace Auth.Controllers
{
    public class TrainingController : Controller
    {
        private readonly DbServices<TrainingModel> _dbService;
        //private readonly SessionHelper _sessionHelper;
        private readonly DbServices<UserModel> _userService;
        private readonly DbServices<NotificationModel> _dbNotificatoionService;
        private readonly StorageService _storageService;
        public TrainingController()
        {
            _dbNotificatoionService = new DbServices<NotificationModel>("notifications");
            _userService = new DbServices<UserModel>("users");
            _dbService = new DbServices<TrainingModel>("trainings");
            _storageService = new StorageService();
            //_sessionHelper = new SessionHelper(accessor);
        }

        public async Task<IActionResult> List()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            var trainings = await _dbService.GetAllAsync();
            var userTrainings = trainings.FindAll(t => t.UserId == userId); 
            return View(userTrainings);
        }

        [HttpGet]
        public IActionResult Add()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            var model = new TrainingModel
            {
                TrainingId = Guid.NewGuid().ToString(),
                UserId = userId, 
                DocumentUrl = "",
                //Status=""
            };

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            var trainings = await _dbService.GetByIdAsync(id);
            return View(trainings);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(TrainingModel model, IFormFile? documentFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            // === 1. Normalize Dates ===
            if (model.StartDate == default)
                model.StartDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            if (model.EndDate == default)
                model.EndDate = DateTime.SpecifyKind(model.StartDate.AddDays(1), DateTimeKind.Utc);

            model.StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
            model.EndDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc);

            if (string.IsNullOrEmpty(model.DocumentUrl))
                model.DocumentUrl = string.Empty;

            if (!ModelState.IsValid)
                return View(model);

            string? uploadedFilePath = null;

            try
            {
                // === 2. Validate & Upload File to Firebase (Only if Completed) ===
                if (model.CompletionStatus == "Completed" && (documentFile == null || documentFile.Length == 0))
                {
                    ModelState.AddModelError("DocumentUrl", "Please upload a certificate when status is Completed.");
                    return View(model);
                }

                if (model.CompletionStatus == "Completed" && documentFile != null && documentFile.Length > 0)
                {
                    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp", "application/pdf" };
                    if (!allowedTypes.Contains(documentFile.ContentType.ToLowerInvariant()))
                    {
                        ModelState.AddModelError("DocumentUrl", "Only JPG, PNG, WebP, and PDF files are allowed.");
                        return View(model);
                    }

                    var guid8 = Guid.NewGuid().ToString("N")[..8];
                    var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                    var ext = Path.GetExtension(documentFile.FileName).ToLowerInvariant();
                    var fileName = $"{userId}_{guid8}_{timestamp}{ext}";

                    // Firebase path: trainings/{uid}/{file}
                    uploadedFilePath = $"{userId}/{fileName}";

                    await using var stream = documentFile.OpenReadStream();
                    var fileUrl = await _storageService.UploadFileAsync(stream, "trainings", uploadedFilePath);

                    model.DocumentUrl = fileUrl; // Full public URL
                }

                // === 3. Save to Firestore ===
                model.TrainingId = Guid.NewGuid().ToString();
                model.UserId = userId;
                model.StartDate = DateTime.UtcNow;
                model.EndDate = model.EndDate;

                var user = await _userService.GetByFieldAsync("userId", userId);
                var notification = new NotificationModel
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    Message = $"A new Training was added by {user?.FullName ?? "User"} on {model.StartDate:MMMM dd, yyyy}.",
                    ReadStatus = "Unread",
                    SentBy = user?.Role ?? "Employee",
                    SentDate = DateTime.UtcNow,
                    UserId = userId
                };

                await _dbNotificatoionService.CreateAsync(notification.NotificationId, notification);
                await _dbService.CreateAsync(model.TrainingId, model);

                TempData["SuccessMessage"] = "Training added successfully!";
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                // === 4. Rollback: Delete uploaded file if failed ===
                if (!string.IsNullOrEmpty(uploadedFilePath))
                {
                    try { await _storageService.DeleteFileAsync("trainings", uploadedFilePath); }
                    catch { /* Log silently */ }
                }

                ModelState.AddModelError("", $"Error: {ex.Message}");
                return View(model);
            }
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
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            var training = await _dbService.GetByIdAsync(id);
            if (training == null)
            {
                return NotFound();
            }
            return View(training);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TrainingModel model, IFormFile? DocumentFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var training = await _dbService.GetByIdAsync(model.TrainingId);
            if (training == null) return NotFound();

            training.Description = model.Description;
            training.Provider = model.Provider;

            if (training.CompletionStatus == "Not Started")
            {
                //if (model.CompletionStatus != "Not Started" &&
                //    model.CompletionStatus != "In Progress" &&
                //    model.CompletionStatus != "Completed")
                //{
                    
                //    ModelState.AddModelError("CompletionStatus", "Invalid status change.");
                //    return View(model);
                //}
            }
            else if (training.CompletionStatus == "In Progress")
            {
                //if (model.CompletionStatus != "In Progress" && model.CompletionStatus != "Completed")
                //{
                //    ModelState.AddModelError("CompletionStatus", "Invalid status change.");
                //    return View(model);
                //}
            }
            else 
            {
                model.CompletionStatus = "Completed"; 
            }

            training.CompletionStatus = model.CompletionStatus;

            if (training.CompletionStatus == "Not Started")
            {
                training.StartDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                training.EndDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            }
            else if (training.CompletionStatus == "In Progress")
            {
                training.StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
                training.EndDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc);
            }
            else
            {
                training.StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
                training.EndDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc);
            }

            if (training.CompletionStatus == "Completed")
            {
                if (DocumentFile != null && DocumentFile.Length > 0)
                {
                    var allowed = new[] { "application/pdf", "image/jpeg", "image/png" };
                    if (!allowed.Contains(DocumentFile.ContentType.ToLowerInvariant()))
                    {
                        ModelState.AddModelError("DocumentFile", "Only PDF, JPG, PNG allowed.");
                        return Content("Type uploaded is the problem");
                        /////////////////////////// do not touch
                        return View(model);
                    }

                    var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "training_docs");
                    Directory.CreateDirectory(folder);

                    var fileName = $"{userId}_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(DocumentFile.FileName)}";
                    var path = Path.Combine(folder, fileName);

                    await using var stream = new FileStream(path, FileMode.Create);
                    await DocumentFile.CopyToAsync(stream);

                    training.DocumentUrl = fileName;
                }
                else
                {
                    /////////////////////////// do not touch
                    return Content("Document is the problem");
                    ModelState.AddModelError("DocumentFile", "Completed trainings require a document.");
                    return View(model);
                }
            }
            var user = await _userService.GetByFieldAsync("userId", userId);
            string helperText = nameof(TrainingModel);
            //var newNotification = new NotificationModel
            //{
            //    NotificationId = Guid.NewGuid().ToString(),
            //    Message = $"A {helperText.Substring(0, helperText.Length - 4)} with UID: {model.TrainingId} was edited on the {DateTime.UtcNow}.",
            //    ReadStatus = "Unread",
            //    SentBy = user!.Role ?? "Employee",
            //    SentDate = DateTime.UtcNow,
            //    UserId = user.UserId
            //};

            var notification = new NotificationModel
            {
                NotificationId = Guid.NewGuid().ToString(),
                Message = $"A {helperText.Substring(0, helperText.Length - 4)} with UID: {model.TrainingId} was edited on the {DateTime.UtcNow}.",
                ReadStatus = "Unread",
                SentBy = user?.Role ?? "Employee",
                SentDate = DateTime.UtcNow,
                UserId = userId
            };
            await _dbNotificatoionService.CreateAsync(notification.NotificationId, notification);

            await _dbService.UpdateAsync(training.TrainingId, training);
            TempData["SuccessMessage"] = "Training updated successfully!";
            return RedirectToAction("List");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            var training = await _dbService.GetByIdAsync(id);
            if (training == null)
            {
                return NotFound();
            }

            return View(training);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(TrainingModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            if (string.IsNullOrEmpty(model.TrainingId))
            {
                return BadRequest("Invalid training ID.");
            }

            await _dbService.DeleteAsync(model.TrainingId);
            TempData["SuccessMessage"] = "Training record removed successfully.";

            return RedirectToAction("List");
        }

    }
}