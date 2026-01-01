using Google.Cloud.Firestore;

namespace auth.Models.Models
{
    [FirestoreData]
    public class NotificationModel
    {
        [FirestoreProperty("notificationId")]
        public string NotificationId { get; set; }
        [FirestoreProperty("userId")]
        public string UserId { get; set; }

        [FirestoreProperty("message")]
        public string Message { get; set; }
        [FirestoreProperty("readStatus")]
        public string ReadStatus { get; set; }
        [FirestoreProperty("sentDate")]
        public DateTime SentDate { get; set; }
        [FirestoreProperty("sentBy")]
        public string SentBy { get; set; }
    }
}
