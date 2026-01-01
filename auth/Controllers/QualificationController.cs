using auth.Models.Models;
using Auth.Models.Models;
using Auth.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace Auth.Controllers
{
    public class QualificationController : Controller
    {
        private readonly DbServices<QualificationModel> _dbService;
        //private readonly SessionHelper _sessionHelper;
        private readonly DbServices<NotificationModel> _dbNotificatoionService;
        private readonly DbServices<UserModel> _userService;
        private readonly StorageService _storageService;
        public QualificationController()
        {
            _userService = new DbServices<UserModel>("users");
            _dbNotificatoionService = new DbServices<NotificationModel>("notifications");
            _dbService = new DbServices<QualificationModel>("qualifications");
            _storageService = new StorageService();
            //_sessionHelper = new SessionHelper(accessor);
        }

        public async Task<IActionResult> List()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            var qualifications = await _dbService.GetAllAsync();
            var userQualification = qualifications.FindAll((q) => q.UserId == userId);

            return View(userQualification);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var qualification = await _dbService.GetByIdAsync(id);
            if (qualification == null)
                return NotFound();

            return View(qualification);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(QualificationModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            if (!ModelState.IsValid)
                return View(model);
            //if (model.InstitutionName != "Other")
            //{
            //    model.InstitutionId = GetInstitutionIdByName(model.InstitutionName);
            //}
            //else if (model.InstitutionName == "Other" && !string.IsNullOrEmpty(Request.Form["InstitutionId"]))
            //{
            //    model.InstitutionId = Request.Form["InstitutionId"]; 
            //}
            //else
            //{
            //    model.InstitutionId = "OTHER"; 
            //}
            model.Status = "Pending";
            model.DateUpdated = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            model.DateAdded = DateTime.SpecifyKind(model.DateAdded, DateTimeKind.Utc);
            var user = await _userService.GetByFieldAsync("userId", userId);
            var notification = new NotificationModel
            {
                NotificationId = Guid.NewGuid().ToString(),
                Message = $"A Qualification with UID: ${model.QualificationId} was edited on the ${model.DateAdded}.",
                ReadStatus = "Unread",
                SentBy = user?.Role ?? "Employee",
                SentDate = DateTime.UtcNow,
                UserId = userId
            };

            await _dbNotificatoionService.CreateAsync(notification.NotificationId, notification);
            await _dbService.UpdateAsync(model.QualificationId, model);

            TempData["SuccessMessage"] = "Qualification updated successfully.";
            return RedirectToAction("List");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var qualification = await _dbService.GetByIdAsync(id);
            if (qualification == null)
                return NotFound();

            return View(qualification);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(QualificationModel model)
        {
            if (string.IsNullOrEmpty(model.QualificationId))
                return BadRequest("Invalid Qualification ID.");

            await _dbService.DeleteAsync(model.QualificationId);
            TempData["SuccessMessage"] = "Qualification deleted successfully.";

            return RedirectToAction("List");
        }


        [HttpGet]
        public IActionResult Add()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            var model = new QualificationModel
            {
                QualificationId = Guid.NewGuid().ToString(),
                UserId = userId,
                DateAdded = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                Status = "Pending",
                CertificationUrl = ""
            };

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var qualification = await _dbService.GetByFieldAsync("qualificationId", id);
            if (qualification == null)
                return NotFound();

            return View(qualification);
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(QualificationModel model, IFormFile? certificationFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            string? uploadedFilePath = null;

            try
            {
                // === 1. Validate & Upload File to Firebase Storage ===
                if (certificationFile == null || certificationFile.Length == 0)
                {
                    ModelState.AddModelError("CertificationUrl", "Please upload your certificate image.");
                    return View(model);
                }

                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
                if (!allowedTypes.Contains(certificationFile.ContentType.ToLowerInvariant()))
                {
                    ModelState.AddModelError("CertificationUrl", "Only JPG, PNG, or WebP files are allowed.");
                    return View(model);
                }

                var guid8 = Guid.NewGuid().ToString("N")[..8];
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var ext = Path.GetExtension(certificationFile.FileName).ToLowerInvariant();
                var fileName = $"{userId}_{guid8}_{timestamp}{ext}";

                // Final Firebase Storage path: qualifications/{uid}/{file}
                uploadedFilePath = $"{userId}/{fileName}";

                await using var stream = certificationFile.OpenReadStream();
                var fileUrl = await _storageService.UploadFileAsync(stream,"qualifications", uploadedFilePath);

                // === 2. Save to Firestore ===
                model.QualificationId = Guid.NewGuid().ToString();
                model.UserId = userId;
                model.DateAdded = DateTime.UtcNow;
                model.DateUpdated = model.DateAdded;
                model.Status = "Pending";
                model.CertificationUrl = fileUrl; // Store full public URL

                var user = await _userService.GetByFieldAsync("userId", userId);
                var notification = new NotificationModel
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    Message = $"A new Qualification was added by {user?.FullName ?? "User"} on {model.DateAdded:MMMM dd, yyyy}.",
                    ReadStatus = "Unread",
                    SentBy = user?.Role ?? "Employee",
                    SentDate = DateTime.UtcNow,
                    UserId = userId
                };

                await _dbNotificatoionService.CreateAsync(notification.NotificationId, notification);
                await _dbService.CreateAsync(model.QualificationId, model);

                TempData["SuccessMessage"] = "Qualification added successfully.";
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                // === 3. Rollback: Delete uploaded file if exists ===
                if (!string.IsNullOrEmpty(uploadedFilePath))
                {
                    try { await _storageService.DeleteFileAsync("qualifications", uploadedFilePath); }
                    catch { /* Log if needed */ }
                }

                ModelState.AddModelError("", $"Error: {ex.Message}");
                return View(model);
            }
        }
        
        //[HttpGet]
        //public IActionResult DownloadCertificate(string fileName)
        //{
        //    if (string.IsNullOrEmpty(fileName))
        //        return NotFound();

        //    var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "certificates");
        //    var filePath = Path.Combine(folder, fileName);

        //    if (!System.IO.File.Exists(filePath))
        //        return NotFound();

        //    var contentType = "application/octet-stream";
        //    return PhysicalFile(filePath, contentType, fileName);
        //}

    }
}