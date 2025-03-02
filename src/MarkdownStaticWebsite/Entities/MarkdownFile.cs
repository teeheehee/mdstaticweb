using Markdig;
using MarkdownStaticWebsite.Modules;
using System.Data.SQLite;
using System.Text;

namespace MarkdownStaticWebsite.Entities
{
    public abstract class MarkdownFile
    {
        public string SourceFilePath { get; }
        public string SourcePath { get; }
        public string BuildOutputPath { get; }
        public string BuildOutputFilePath { get; }
        public string RelativeFilePath { get; }
        public string BaseMarkdownFilename { get; }
        public string BaseHtmlFilename { get; }
        public string BaseUrl { get; }
        public string RelativeUrlPath { get; }
        public string RelativeUrl { get; }
        public string Url { get; }
        public string MarkdownContent { get; }
        public string MarkdownHtmlContent { get; }
        public string HtmlContent { get { return RenderedHtmlContent; } }
        public string TemplateType { get; }
        public IDictionary<string, string> ReplacementTagValues { get; }

        protected IEnumerable<string>? RendertimeTags { get; set; }
        protected string RenderedHtmlContent { get; set; }

        protected const string MarkdownContentTag = "markdown-content";
        protected const string PageUrlTag = "page-url";

        public MarkdownFile(
            string sourceFilePath,
            string sourcePath,
            string buildOutputPath,
            string baseUrl,
            string markdownContent,
            string templateType,
            IDictionary<string, string> yamlFileContentReplacementTagValues,
            IDictionary<string, string> templateFileNameReplacementTagValues,
            IDictionary<string, string> dbReplacementTagValues)
        {
            SourceFilePath = sourceFilePath;
            SourcePath = new DirectoryInfo(sourcePath).ToString();
            BuildOutputPath = new DirectoryInfo(buildOutputPath).ToString();

            SourcePath = Path.EndsInDirectorySeparator(SourcePath)
                ? SourcePath
                : $"{SourcePath}{Path.DirectorySeparatorChar}";
            BuildOutputPath = Path.EndsInDirectorySeparator(BuildOutputPath)
                ? BuildOutputPath
                : $"{BuildOutputPath}{Path.DirectorySeparatorChar}";

            var sourceFile = new FileInfo(SourceFilePath);

            BaseMarkdownFilename = sourceFile.Name;
            BaseHtmlFilename = BaseMarkdownFilename.Replace(".md", ".html");

            // Later on, Path.Combine gets used and it can be a bit messy to work with so we need to do some string manipulations.
            // Path.Combine doesn't play well with relative paths that look like absolute paths in 2nd, 3rd, ... parameters,
            // it'll just skip the first parameter in those cases
            RelativeFilePath = $@"{sourceFile.Directory?.ToString().Replace(Path.TrimEndingDirectorySeparator(SourcePath), "")}"
                .TrimStart(Path.DirectorySeparatorChar);

            BuildOutputFilePath = Path.Combine(BuildOutputPath, RelativeFilePath, BaseHtmlFilename);
            BaseUrl = baseUrl.TrimEnd('/');
            RelativeUrlPath = "/" + RelativeFilePath.Replace($"{Path.DirectorySeparatorChar}", "/") + (RelativeFilePath.Length > 0 ? "/" : "");

            var fullUrl = new Uri(BaseUrl + RelativeUrlPath + BaseHtmlFilename);
            Url = fullUrl.AbsoluteUri;
            RelativeUrl = fullUrl.AbsolutePath;

            MarkdownContent = markdownContent;
            ReplacementTagValues = yamlFileContentReplacementTagValues;
            TemplateType = templateType;

            EnsureSafeTemplateTree(TemplateType, templateFileNameReplacementTagValues, new List<string>());

            var templateContents = Parser.ConstructFullTemplate(0, TemplateType, templateFileNameReplacementTagValues);

            // categorize available replacement tags
            // we replace in priority order from most to least significant

            var replacementTags = Parser.GetReplacementTagsFromTemplateContents(templateContents);

            var markdownReplacementTags = replacementTags.Where(rt => ReplacementTagValues.ContainsKey(rt));
            var dbReplacementTags = replacementTags.Where(rt => dbReplacementTagValues.ContainsKey(rt));

            var combinedReplacementTags = new Dictionary<string, string>();

            foreach (var markdownReplacementTag in markdownReplacementTags)
            {
                combinedReplacementTags.Add(markdownReplacementTag, ReplacementTagValues[markdownReplacementTag]);
            }

            foreach (var dbReplacementTag in dbReplacementTags)
            {
                if (!combinedReplacementTags.ContainsKey(dbReplacementTag))
                {
                    combinedReplacementTags.Add(dbReplacementTag, dbReplacementTagValues[dbReplacementTag]);
                }
            }

            // apply tag replacements that are available, insert markdown rendered HTML

            templateContents = Parser.ApplyTagReplacements(templateContents, combinedReplacementTags);

            var numberOfMarkdownTabs = Parser.GetNumberOfTabsPriorToSearchString(MarkdownContentTag, templateContents);

            var markdownPipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter() // strip YAML content prior to HTML rendering
                .UseAutoIdentifiers() // adds IDs to header tags with unique value
                .Build();

            // Fix line endings from Html process, and indent for applying to the template
            MarkdownHtmlContent = Parser.IndentContent(
                numberOfMarkdownTabs,
                Markdown.ToHtml(MarkdownContent, markdownPipeline).Replace("\n", Environment.NewLine));

            templateContents = templateContents
                .Replace(Parser.GetTagReplacementSearchString(MarkdownContentTag), MarkdownHtmlContent)
                .Replace(Parser.GetTagReplacementSearchString(PageUrlTag), Url);

            RenderedHtmlContent = templateContents;
        }

        public abstract IEnumerable<SQLiteCommand> GetUpsertCommands(SQLiteConnection connection);

        public abstract IEnumerable<SQLiteCommand> GetLinkingTableUpsertCommands(SQLiteConnection connection);

        protected void EnsureAllPrerenderTagsAreReplaced()
        {
            var remainingTags = Parser.GetReplacementTagsFromTemplateContents(HtmlContent);

            var unhandledTags = remainingTags
                .Where(rt => !(RendertimeTags ?? new List<string>())
                .Contains(rt))
                .ToList();

            if (unhandledTags.Any())
            {
                throw new Exception($"Did not replace all pre-renderable tags in file {SourceFilePath}: {string.Join(",", unhandledTags)}");
            }

        }

        protected void EnsureAllTagsAreReplaced()
        {
            var remainingTags = Parser.GetReplacementTagsFromTemplateContents(HtmlContent);

            if (remainingTags.Count() > 0)
            {
                throw new Exception($"Did not replace all tags in file {SourceFilePath}: {string.Join(",", remainingTags)}");
            }
        }

        public void WriteOutputFile(IDictionary<string, string> finalTagReplacements)
        {
            RenderedHtmlContent = Parser.ApplyTagReplacements(HtmlContent, ReplacementTagValues);
            RenderedHtmlContent = Parser.ApplyTagReplacements(HtmlContent, finalTagReplacements);

            // Check our work before committing to outputting the file
            EnsureAllTagsAreReplaced();

            if (!Directory.Exists(BuildOutputPath + RelativeFilePath))
            {
                Directory.CreateDirectory(BuildOutputPath + RelativeFilePath);
            }

            using (FileStream fs = File.Create(BuildOutputFilePath))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(HtmlContent);
                fs.Write(info, 0, info.Length);
            }
        }

        #region Private helper methods


        private static void EnsureSafeTemplateTree(
            string templateType,
            IDictionary<string, string> templateFileNameReplacementTagValues,
            IEnumerable<string> encounteredTemplateReplacementTags)
        {
            var templateFile = templateFileNameReplacementTagValues[templateType];
            var replacementTags = Parser.GetReplacementTagsFromTemplateFile(templateFile);

            var templatesInTemplate = replacementTags
                .Where(rt => templateFileNameReplacementTagValues.ContainsKey(rt))
                .ToList();

            if (templatesInTemplate.Any(t => encounteredTemplateReplacementTags.Contains(t)))
            {
                throw new Exception($"Possible recursive loop in unpacking templates-within-templates: {templateFile}");
            }

            var updatedEncounteredTemplateReplacementTags = new List<string>();
            updatedEncounteredTemplateReplacementTags.AddRange(encounteredTemplateReplacementTags);
            updatedEncounteredTemplateReplacementTags.AddRange(templatesInTemplate);

            foreach (var template in templatesInTemplate)
            {
                EnsureSafeTemplateTree(
                    template,
                    templateFileNameReplacementTagValues,
                    updatedEncounteredTemplateReplacementTags);
            }
        }

        #endregion
    }
}
