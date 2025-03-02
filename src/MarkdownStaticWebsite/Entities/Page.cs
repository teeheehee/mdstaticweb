using MarkdownStaticWebsite.Modules;
using System.Data.SQLite;

namespace MarkdownStaticWebsite.Entities
{
    public class Page : MarkdownFile
    {
        public Page(
            string sourceFilePath,
            string sourcePath,
            string buildOutputPath,
            string baseUrl,
            string markdownContent,
            string templateType,
            IDictionary<string, string> yamlFileContentReplacementTagValues,
            IDictionary<string, string> templateFileNameReplacementTagValues,
            IDictionary<string, string> dbReplacementTagValues)
            : base(
                sourceFilePath,
                sourcePath,
                buildOutputPath,
                baseUrl,
                markdownContent,
                templateType,
                yamlFileContentReplacementTagValues,
                templateFileNameReplacementTagValues,
                dbReplacementTagValues)
        {
            // Defer tag replacement for things that need to be prepared later and applied at render time
            var rendertimeTags = new List<string>
            {
                "navigation-link-items"
            };

            RendertimeTags = rendertimeTags;

            EnsureAllPrerenderTagsAreReplaced();
        }

        public override IEnumerable<SQLiteCommand> GetUpsertCommands(SQLiteConnection connection)
        {
            var results = new List<SQLiteCommand>();

            var showInNavigation = YamlHelpers.ConvertYamlStringToBool(
                YamlHelpers.GetYamlValue("show-in-navigation", ReplacementTagValues, "no"));
            var navigationPosition = YamlHelpers.ConvertYamlStringToInt(
                YamlHelpers.GetYamlValue("navigation-position", ReplacementTagValues)) ?? 0;

            results.Add(new UpsertRequest
            {
                TableName = "Pages",
                CollisionField = "Url",
                ColumnValuePairs = new Dictionary<string, object>
                    {
                        { "DateCreated", DateTime.Now.ToString("o") },
                        { "Filename", BaseMarkdownFilename },
                        { "RelativePath", RelativeUrlPath },
                        { "RelativeUrl", RelativeUrl },
                        { "Url", Url },
                        { "Title", ReplacementTagValues["title"] },
                        { "IncludeInNavigation", showInNavigation },
                        { "NavigationPosition", navigationPosition }
                    }
            }.GetUpsertCommand(connection));

            return results;
        }

        public override IEnumerable<SQLiteCommand> GetLinkingTableUpsertCommands(SQLiteConnection connection)
        {
            return new List<SQLiteCommand>();
        }
    }
}
