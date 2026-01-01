using Google.Cloud.Firestore;

namespace Auth.Models.Models
{
    [FirestoreData] 
    public class UserModel
    {
        [FirestoreProperty("userId")]
        public string UserId { get; set; }

        [FirestoreProperty("fullName")]
        public string FullName { get; set; }

        [FirestoreProperty("email")]
        public string Email { get; set; }

        [FirestoreProperty("password")]
        public string Password { get; set; } 

        [FirestoreProperty("role")]
        public string Role { get; set; }

        [FirestoreProperty("department")]
        public string Department { get; set; }

        [FirestoreProperty("position")]
        public string Position { get; set; }

        [FirestoreProperty("profilePicture")]
        public string ProfilePicture { get; set; }

        [FirestoreProperty("lastLogin")]
        public DateTime LastLogin { get; set; } = DateTime.UtcNow;

        [FirestoreProperty("status")]
        public string Status { get; set; }

        [FirestoreProperty("dateAdded")]
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        [FirestoreProperty("dateUpdated")]
        public DateTime DateUpdated { get; set; } = DateTime.UtcNow;

        //[FirestoreProperty("skills")]
        //public List<SkillModel> Skills { get; set; } = new();
        //[FirestoreProperty("trainings")]
        //public List<TrainingModel> Trainings { get; set; } = new();

        //[FirestoreProperty("qualifications")]
        //public List<QualificationModel> Qualifications { get; set; } = new();
    }
}