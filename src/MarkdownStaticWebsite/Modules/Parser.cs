using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownStaticWebsite.Modules
{
    public static class Parser
    {

        public const string ReplacementTagRegexSearch = @"\${(.*?)}";
        public static Regex ReplacementTagRegex = new(
            ReplacementTagRegexSearch,
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #region String helpers

        private static string GetTabs(int numberOfTabs)
        {
            var tabs = "";
            for (int i = 0; i < numberOfTabs; i++)
            {
                tabs += "\t";
            }
            return tabs;
        }

        public static string IndentContent(int numberOfTabs, string content)
        {
            var prefixTabs = GetTabs(numberOfTabs);
            var sb = new StringBuilder();

            foreach (var line in content.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.None))
            {
                sb.AppendLine($"{prefixTabs}{line}");
            }

            // Trimming the start because this will get Replace'd into an already indented position
            // Trimming the end to avoid introducing new NewLine's
            return sb.ToString().Trim();
        }

        public static int GetNumberOfTabsPriorToSearchString(string searchString, string content)
        {
            int result = 0;

            var start = content.IndexOf(GetTagReplacementSearchString(searchString)) - 1;
            while (start > 0 && content[start] == '\t')
            {
                result++;
                start--;
            }

            return result;
        }

        private static string GetIndentedTemplateContents(int numberOfTabs, string templateFile)
        {
            return IndentContent(numberOfTabs, File.ReadAllText(templateFile));
        }

        #endregion

        #region Replacements

        public static string GetTagReplacementSearchString(string tag)
        {
            // THIS ASSUMES TAG SYNTAX
            return $"${{{tag}}}";
        }

        internal static IEnumerable<string> GetReplacementTagsFromTemplateContents(string contents)
        {
            var results = new List<string>();

            var lines = contents.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                var matches = ReplacementTagRegex.Matches(line);

                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        // get the matched search string that is inside the tag syntax
                        var groups = match.Groups;
                        // TODO: only run regex searches in non-codeblock elements (AngleSharp)
                        // Omit finding things in a code example
                        if (groups[1].Value == "(.*?)")
                        {
                            continue;
                        }
                        results.Add(groups[1].Value);
                    }
                }
            }

            var uniqueResults = results.Distinct().ToList();

            //if (results.Any())
            //{
            //	$"Contents had {results.Count} replacement tags, {uniqueResults.Count} unique tags".Dump();
            //}
            //else
            //{
            //	"No replacement tags were found".Dump();
            //}

            return uniqueResults;
        }

        internal static IEnumerable<string> GetReplacementTagsFromTemplateFile(string filePath)
        {
            return GetReplacementTagsFromTemplateContents(File.ReadAllText(filePath));
        }

        internal static string ApplyTagReplacements(string content, IDictionary<string, string> replacementTags)
        {
            var results = content;
            foreach (var replacementTag in replacementTags.Keys)
            {
                var numberOfTabs = GetNumberOfTabsPriorToSearchString(replacementTag, results);
                var replacementText = IndentContent(numberOfTabs, replacementTags[replacementTag]);
                var searchString = GetTagReplacementSearchString(replacementTag);

                results = results.Replace(
                    searchString,
                    replacementText);
            }
            return results;
        }

        #endregion

        #region Templates

        public static string ConstructFullTemplate(
            int numberOfTabs,
            string templateType,
            IDictionary<string, string> templateFileNameReplacementTagValues)
        {
            var templateContents = GetIndentedTemplateContents(numberOfTabs, templateFileNameReplacementTagValues[templateType]);

            var replacementTags = GetReplacementTagsFromTemplateContents(templateContents);
            var templatesInTemplate = replacementTags
                .Where(rt => templateFileNameReplacementTagValues.ContainsKey(rt))
                .ToList();

            foreach (var template in templatesInTemplate)
            {
                var fullNumberOfTabs = numberOfTabs + GetNumberOfTabsPriorToSearchString(template, templateContents);

                var subTemplateContents = ConstructFullTemplate(fullNumberOfTabs, template, templateFileNameReplacementTagValues);

                templateContents = templateContents.Replace(
                    GetTagReplacementSearchString(template),
                    subTemplateContents);
            }

            return templateContents;
        }

        #endregion

    }
}
