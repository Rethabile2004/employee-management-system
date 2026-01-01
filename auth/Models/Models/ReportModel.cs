using Google.Cloud.Firestore;
namespace auth.Models.Models
{

    [FirestoreData]
    public class ReportModel
    {
        [FirestoreDocumentId]
        public string ReportId { get; set; }

        [FirestoreProperty("userId")]
        public string UserId { get; set; }

        [FirestoreProperty("title")]
        public string Title { get; set; }

        [FirestoreProperty("description")]
        public string Description { get; set; }

        [FirestoreProperty("submittedAt")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [FirestoreProperty("status")]
        public string Status { get; set; } = "Submitted"; // Submitted, Reviewed, etc.
    }
}
