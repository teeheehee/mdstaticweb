
namespace MarkdownStaticWebsite.Modules
{
    public static class FileHelpers
    {
        public static IEnumerable<string> GetTemplateFiles(string path)
        {
            //"Getting all template files".Dump();
            return Directory.GetFiles(path, "*.html", SearchOption.AllDirectories).ToList();
        }

        public static IEnumerable<string> GetAllSourceFiles(string sourcePath)
        {
            return Directory.EnumerateFiles(sourcePath, "*.*", SearchOption.AllDirectories).ToList();
        }

        public static IEnumerable<string> GetMarkdownFiles(IEnumerable<string> sourceFiles)
        {
            return sourceFiles.Where(f => f.ToLowerInvariant().EndsWith(".md")).ToList();
        }

        public static IEnumerable<string> GetImageFilesToProcess(
            IEnumerable<string> sourceFiles,
            IDictionary<string, string> dbReplacementTagValues)
        {
            var imageFileExtensions = new List<string>()
            {
                ".jpg",
                ".jpeg",
                ".gif",
                ".png",
            };

            var results = new List<string>();

            foreach (var extension in imageFileExtensions)
            {
                var relatedFiles = sourceFiles.Where(f => f.ToLowerInvariant().EndsWith(extension));

                // remove images that are favicons
                foreach (var replacement in dbReplacementTagValues.Where(kvp => kvp.Value.EndsWith(extension)))
                {
                    relatedFiles = relatedFiles.Where(rf => !rf.Contains(replacement.Value.Replace('/', '\\')));
                }

                results.AddRange(relatedFiles);
            }

            return results.ToList();
        }

        public static IDictionary<string, string> GetTemplateFileNameReplacementTagValues(IEnumerable<string> filePaths)
        {
            //"Getting template file names as replacement tags".Dump();

            var results = new Dictionary<string, string>();

            foreach (var filePath in filePaths)
            {
                var nameReplacementTag = new FileInfo(filePath).Name.Replace(".html", "");
                results.Add(nameReplacementTag, filePath);
            }

            return results;
        }
    }
}
