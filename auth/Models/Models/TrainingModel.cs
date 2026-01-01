using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Models
{
    [FirestoreData]
    public class TrainingModel
    {
        [FirestoreProperty("trainingId")]
        public string TrainingId { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;
        [Required]
        [FirestoreProperty("trainingName")]
        public string TrainingName { get; set; } = string.Empty;
        [Required]
        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("startDate")]
        public DateTime StartDate { get; set; }

        [FirestoreProperty("endDate")]
        public DateTime EndDate { get; set; }

        [FirestoreProperty("documentationUrl")]
        public string DocumentUrl { get; set; } = string.Empty;

        [FirestoreProperty("completionStatus")]
        public string CompletionStatus { get; set; } = "Not Started";
        [Required]
        [FirestoreProperty("provider")]
        public string Provider { get; set; } = string.Empty;
    }
}
