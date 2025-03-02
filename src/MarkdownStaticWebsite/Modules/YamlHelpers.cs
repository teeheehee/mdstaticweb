
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization.NamingConventions;

namespace MarkdownStaticWebsite.Modules
{
    public static class YamlHelpers
    {
        public static string GetYamlValue(
            string key,
            IDictionary<string, string> dictionary,
            string defaultValue = "")
        {
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }
            return defaultValue;
        }

        public static bool ConvertYamlStringToBool(string value)
        {
            var checkValue = value.ToLowerInvariant();

            switch (checkValue)
            {
                case "y":
                case "yes":
                case "t":
                case "true":
                case "1":
                    return true;
                default:
                    return false;
            }
        }

        public static int? ConvertYamlStringToInt(string value)
        {
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public static IDictionary<string, string> GetYamlFileContentReplacementTagValues(string content)
        {
            var results = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(content)) return results;

            // https://markheath.net/post/markdown-html-yaml-front-matter
            var yamlDeserializer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            using (var input = new StringReader(content))
            {
                var parser = new YamlDotNet.Core.Parser(input);
                parser.Consume<StreamStart>();
                parser.Consume<DocumentStart>();
                var yaml = yamlDeserializer.Deserialize<Dictionary<string, string>>(parser);
                parser.Consume<DocumentEnd>();

                foreach (var kvp in yaml)
                {
                    results.Add(GetReplacementTagFromYamlTag(kvp.Key), kvp.Value);
                }
            }

            return results;
        }

        private static string GetReplacementTagFromYamlTag(string yamlTag)
        {
            var result = yamlTag.ToLowerInvariant();
            return Regex.Replace(result, @"\s", "-");
        }
    }
}
