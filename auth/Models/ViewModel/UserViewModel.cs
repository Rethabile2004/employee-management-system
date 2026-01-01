using Google.Cloud.Firestore;

namespace Auth.Models.ViewModels
{
    [FirestoreData]
    public class UserViewModel
    {
        [FirestoreProperty]
        public string UserId { get; set; }
        [FirestoreProperty]
        public string FullName { get; set; }
        public int AuditCount { get; set; }
    }
}