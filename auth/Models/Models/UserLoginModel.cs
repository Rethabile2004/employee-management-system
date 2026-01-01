using Google.Cloud.Firestore;

namespace Streamline.Models.ViewModels
{
    [FirestoreData]
    public class UserLoginModel
    {
        [FirestoreProperty("userId")]
        public string? UserId { get; set; }

        [FirestoreProperty("email")]
        public string Email { get; set; } = string.Empty;

        [FirestoreProperty("password")]
        public string Password { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        public string AccountStatus { get; set; } = string.Empty;
        [FirestoreProperty("role")]
        public string Role { get; set; } = string.Empty;
    }
}