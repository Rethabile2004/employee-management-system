using Auth.Models.Models;
using Auth.Models.NewFolder;

namespace Auth.Models.ViewModels
{
    public class SkillGapViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public List<SkillModel> MissingSkills { get; set; } = new();
    }
}
