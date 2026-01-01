using Google.Cloud.Firestore;

namespace Auth.Models.Models
{
    [FirestoreData]
    public class SkillModel 
    {
        [FirestoreProperty("skillId")]
        public string SkillId { get; set; }
        =string.Empty;
        [FirestoreProperty("userId")]
        public string UserId { get; set; }=string.Empty;
        [FirestoreProperty("skillName")]
        public string SkillName { get; set; }=string.Empty;
        [FirestoreProperty("skillLevel")]
        public string SkillLevel { get; set; }=string.Empty;
        [FirestoreProperty("category")]
        public string Category { get; set; }=string.Empty;
        [FirestoreProperty("status")]
        public string Status { get; set; }=string.Empty;
        [FirestoreProperty("approvedBy")]
        public string ApprovedBy { get; set; }=string.Empty;
        [FirestoreProperty("dateAdded")]
        public DateTime DateAdded { get; set; }
        [FirestoreProperty("dateUpdated")]
        public DateTime DateUpdated { get; set; }
    }
}