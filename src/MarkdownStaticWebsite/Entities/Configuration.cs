
namespace MarkdownStaticWebsite.Entities
{
    public class Configuration
    {
        private static readonly string DefaultBasePath = AppContext.BaseDirectory ?? @"C:\dev\website\";
        private static readonly string DefaultDbFile = Path.Combine(DefaultBasePath, "website.sqlite3");
        private static readonly string DefaultTemplatesPath = Path.Combine(DefaultBasePath, "templates");
        private static readonly string DefaultSourcePath = Path.Combine(DefaultBasePath, "src");
        private static readonly string DefaultBuildOutputPath = Path.Combine(DefaultBasePath, "build");

        public string DatabaseFile { get; set; } = DefaultDbFile;
        public string TemplatesFilesPath { get; set; } = DefaultTemplatesPath;
        public string ContentSourcePath { get; set; } = DefaultSourcePath;
        public string BuildSiteOutputPath { get; set; } = DefaultBuildOutputPath;

        public Configuration() { }

    }
}
