
namespace MarkdownStaticWebsite.Modules
{
    public static class FileHelpers
    {
        /// <summary>
        /// Finds all "*.html" files in a directory tree and returns a list of fully qualified
        /// path + filename references to them.
        /// </summary>
        public static IEnumerable<string> GetTemplateFiles(string path)
        {
            return Directory.GetFiles(path, "*.html", SearchOption.AllDirectories).ToList();
        }

        /// <summary>
        /// Finds all "*.*" files in a directory tree and returns a list of fully qualified
        /// path + filename references to them.
        /// </summary>
        public static IEnumerable<string> GetAllSourceFiles(string sourcePath)
        {
            return Directory.EnumerateFiles(sourcePath, "*.*", SearchOption.AllDirectories).ToList();
        }

        /// <summary>
        /// Finds all "*.md" markdown files in a directory tree and returns a list of fully qualified
        /// path + filename references to them.
        /// </summary>
        public static IEnumerable<string> GetMarkdownFiles(IEnumerable<string> sourceFiles)
        {
            return sourceFiles.Where(f => f.ToLowerInvariant().EndsWith(".md")).ToList();
        }


        /// <summary>
        /// Finds all "*.jpg, *.jpeg, *.gif, *.png" image files in a directory tree and returns
        /// a list of fully qualified path + filename references to them.
        /// </summary>
        public static IEnumerable<string> GetImageFilesToProcess(string imagesSourcePath)
        {
            var filesInImagesPath = Directory.EnumerateFiles(imagesSourcePath, "*.*", SearchOption.AllDirectories).ToList();

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
                var relatedFiles = filesInImagesPath.Where(f => f.ToLowerInvariant().EndsWith(extension));

                results.AddRange(relatedFiles);
            }

            return results.ToList();
        }

        /// <summary>
        /// Gets a dictionary of replacement tags and the corresponding template files.
        /// </summary>
        public static IDictionary<string, string> GetTemplateFileNameReplacementTagValues(
            IEnumerable<string> filePaths)
        {
            var results = new Dictionary<string, string>();

            foreach (var filePath in filePaths)
            {
                var nameReplacementTag = new FileInfo(filePath).Name.Replace(".html", "");
                results.Add(nameReplacementTag, filePath);
            }

            return results;
        }

        /// <summary>
        /// Computes MD5 checksum for a file.
        /// Copied from: https://makolyte.com/csharp-get-a-files-checksum-using-any-hashing-algorithm-md5-sha256/
        /// </summary>
        public static string GetMD5Checksum(string filename)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
        }
    }
}
