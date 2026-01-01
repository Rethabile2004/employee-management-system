using Google.Cloud.Firestore;

namespace Auth.Models.Models
{
    [FirestoreData]
    public class RecommendedTrainingModel
    {
        [FirestoreProperty("document")]
        public string Document { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("trainingId")]
        public string TrainingId { get; set; } = string.Empty;

        [FirestoreProperty("trainingName")]
        public string TrainingName { get; set; } = string.Empty;

        [FirestoreProperty("reason")]
        public string Reason { get; set; }=   string.Empty;

        [FirestoreProperty("status")]
        public string Status { get; set; } = string.Empty;          // pending, accepted, completed, declined

        [FirestoreProperty("dateSent")]
        public DateTime DateSent { get; set; } 

        [FirestoreProperty("priority")]
        public string Priority { get; set; }=   string.Empty;            // high, medium, low

        // NEW FIELDS
        [FirestoreProperty("dateApproved")]
        public DateTime? DateApproved { get; set; }=DateTime.UtcNow;        // null until HR/employee approves

        [FirestoreProperty("dateCompleted")]
        public DateTime? DateCompleted { get; set; } = DateTime.UtcNow;      // null until finished
    }
}