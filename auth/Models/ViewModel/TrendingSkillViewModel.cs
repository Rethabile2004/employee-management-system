namespace Auth.Models.ViewModels
{
    public class TrendingSkillsViewModel
    {
        public Dictionary<string, List<string>> Categories { get; set; }
        public string SkillName { get; set; }
        public int UserCount { get; set; }

        public Stream Department { get; set; }

        public string Level { get; set; }        // Beginner, Intermediate, Advanced
        public string Description { get; set; }
    }
}
