using auth.Models.Models;
using Auth.Models.Models;

namespace Auth.Models.ViewModels
{
    public class UserDetailsViewModel
    {
        public UserModel User { get; set; }
        public List<SkillModel> Skills { get; set; }
        public List<TrainingModel> Trainings { get; set; }
        public List<QualificationModel> Qualifications { get; set; }
    }
}