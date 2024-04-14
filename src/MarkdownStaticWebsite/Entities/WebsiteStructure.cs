
namespace MarkdownStaticWebsite.Entities
{
    public class WebsiteStructure
    {
        public IEnumerable<string> TemplateFiles { get; set; }
        public IEnumerable<string> AllSourceFiles { get; set; }
        public IEnumerable<string> ImageFilesToProcess { get; set; }
        public IEnumerable<string> MarkdownFilesToProcess { get; set; }
        public IEnumerable<string> FilesToCopyAsIs { get; set; }
        public IDictionary<string, string> TemplateFileNameReplacementTagValues { get; set; }
        public IDictionary<string, string> DbReplacementTagValues { get; set; }
    }
}
