using Google.Cloud.Firestore;

namespace Auth.Models.ViewModels
{
    [FirestoreData]
    public class AuditViewModel
    {
        [FirestoreProperty]
        public string AuditId { get; set; }

        [FirestoreProperty]
        public string UserId { get; set; }

        [FirestoreProperty]
        public DateTime Date { get; set; }

        [FirestoreProperty]
        public string AuditedBy { get; set; }
        [FirestoreProperty]
        public string Action{ get; set; }
    }
}