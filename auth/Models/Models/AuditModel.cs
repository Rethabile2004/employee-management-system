using Google.Cloud.Firestore;

namespace Auth.Models.NewFolder
{
    [FirestoreData]
    public class AuditModel
    {
        [FirestoreProperty]
        public string AuditId { get; set; }
        [FirestoreProperty]
        public string UserId { get; set; }
        [FirestoreProperty]
        public string Action { get; set; }
        [FirestoreProperty]
        public string Details {  get; set; }
        [FirestoreProperty]
        public DateTime Date {  get; set; }
        [FirestoreProperty]
        public string AuditedBy { get; set; }
    }
}