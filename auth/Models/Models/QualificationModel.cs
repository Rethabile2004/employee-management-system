using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;
namespace auth.Models.Models
{
    [FirestoreData]
    public class QualificationModel
    {
        [FirestoreProperty("qualificationId")]
        public string QualificationId { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("qualificationName")]
        [Required(ErrorMessage = "Qualification name is required.")]
        [StringLength(100, ErrorMessage = "Qualification name cannot exceed 100 characters.")]
        public string QualificationName { get; set; } = string.Empty;

        [FirestoreProperty("completionYear")]
        [Required(ErrorMessage = "Completion year is required.")]
        [Range(1900, 2025, ErrorMessage = "Year must be between 1900 and the current year.")]
        public int CompletionYear { get; set; }

        [FirestoreProperty("certificationUrl")]
        [StringLength(200, ErrorMessage = "Certification URL cannot exceed 200 characters.")]
        public string CertificationUrl { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        [Required(ErrorMessage = "Status is required.")]
        [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters.")]
        public string Status { get; set; } = "Pending";

        [FirestoreProperty("dateAdded")]
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        [FirestoreProperty("dateUpdated")]
        public DateTime DateUpdated { get; set; }

        [FirestoreProperty("institutionName")]
        public string InstitutionName { get; set; } = string.Empty;
    }

}