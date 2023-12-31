
namespace MarkdownStaticWebsite.Entities
{
    public class Configuration
    {
        private static readonly string DefaultBasePath = AppContext.BaseDirectory ?? @"C:\dev\website\";
        //private static readonly string DefaultConfigurationName = AppDomain.CurrentDomain.FriendlyName.Replace(".exe", ".json");
        //private static readonly string DefaultConfigurationName = "defaultConfig.json";
        //private static readonly string DefaultConfigurationFile = DefaultBasePath + @$"{DefaultConfigurationName}";
        private static readonly string DefaultDbFile = DefaultBasePath + @"website.sqlite3";
        private static readonly string DefaultTemplatePath = DefaultBasePath + @"templates";
        private static readonly string DefaultSourcePath = DefaultBasePath + @"src";
        private static readonly string DefaultBuildOutputPath = DefaultBasePath + @"build";

        //[JsonPropertyName("BasePath")]
        public string BasePath { get; set; } = DefaultBasePath;
        //[JsonPropertyName("DbFile")]
        public string DbFile { get; set; } = DefaultDbFile;
        //[JsonPropertyName("TemplatePath")]
        public string TemplatePath { get; set; } = DefaultTemplatePath;
        //[JsonPropertyName("SourcePath")]
        public string SourcePath { get; set; } = DefaultSourcePath;
        //[JsonPropertyName("BuildOutputPath")]
        public string BuildOutputPath { get; set; } = DefaultBuildOutputPath;

        public Configuration() { }

    }
}
