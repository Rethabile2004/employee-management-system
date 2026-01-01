using Google.Cloud.Firestore;

namespace Auth.Models.ViewModels
{
    public class UserHRViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }

        public int NumberOfReports { get; set; }

        public int TrainingsCompleted { get; set; }
        public int TrainingsPending { get; set; }
        public int TotalTrainings => TrainingsCompleted + TrainingsPending;

        public string TrainingStatus { get; set; }

        public int AuditCount { get; set; }

        public bool IsApproved { get; set; }
        [FirestoreProperty("status")]
        public string AccountStatus { get; set; }
    }
}