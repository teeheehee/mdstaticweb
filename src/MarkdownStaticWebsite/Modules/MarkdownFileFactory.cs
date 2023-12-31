using MarkdownStaticWebsite.Entities;

namespace MarkdownStaticWebsite.Modules
{
    public static class MarkdownFileFactory
    {
        public static MarkdownFile GetMarkdownFile(
            string sourceFilePath,
            string sourcePath,
            string buildOutputPath,
            string baseUrl,
            IDictionary<string, string> templateFileNameReplacementTagValues,
            IDictionary<string, string> dbReplacementTagValues)
        {
            var markdownContent = File.ReadAllText(sourceFilePath);
            var yamlFileContentReplacementTagValues = YamlHelpers.GetYamlFileContentReplacementTagValues(markdownContent);

            var templateType = yamlFileContentReplacementTagValues["type"].ToLower();

            switch (templateType)
            {
                case "post":
                    return new Post(
                        sourceFilePath,
                        sourcePath,
                        buildOutputPath,
                        baseUrl,
                        markdownContent,
                        templateType,
                        yamlFileContentReplacementTagValues,
                        templateFileNameReplacementTagValues,
                        dbReplacementTagValues
                    );
                case "page":
                default:
                    return new Page(
                        sourceFilePath,
                        sourcePath,
                        buildOutputPath,
                        baseUrl,
                        markdownContent,
                        templateType,
                        yamlFileContentReplacementTagValues,
                        templateFileNameReplacementTagValues,
                        dbReplacementTagValues
                    );
            }
        }
    }
}
