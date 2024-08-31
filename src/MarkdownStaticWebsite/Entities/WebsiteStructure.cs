
namespace MarkdownStaticWebsite.Entities
{
    public class WebsiteStructure
    {
        public IEnumerable<string> TemplateFiles { get; set; } = new List<string>();
        public IEnumerable<string> AllSourceFiles { get; set; } = new List<string>();
        public IEnumerable<string> ImageFilesToProcess { get; set; } = new List<string>();
        public IEnumerable<string> MarkdownFilesToProcess { get; set; } = new List<string>();
        public IEnumerable<string> FilesToCopyAsIs { get; set; } = new List<string>();
        public IDictionary<string, string> TemplateFileNameReplacementTagValues { get; set; } = new Dictionary<string, string>();
        public IDictionary<string, string> DbReplacementTagValues { get; set; } = new Dictionary<string, string>();
    }
}
