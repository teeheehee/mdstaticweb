
using MarkdownStaticWebsite.Entities;
using MarkdownStaticWebsite.Modules;
using MarkdownStaticWebsite.Repositories;

namespace MarkdownStaticWebsite.Services
{
    public class WebsiteService
    {
        public WebsiteService() { }

        public static WebsiteStructure ProcessTemplates()
        {
            var templateFiles = FileHelpers.GetTemplateFiles(
                ConfigurationService.GetService().Configuration.TemplatesFilesPath);

            var templateFileNameReplacementTagValues = FileHelpers
                .GetTemplateFileNameReplacementTagValues(templateFiles);

            var dbReplacementTagValues = WebsiteData
                .GetDatabaseReplacementTagValues(ConfigurationService.GetService().Configuration.DatabaseFile);

            var allSourceFiles = FileHelpers
                .GetAllSourceFiles(ConfigurationService.GetService().Configuration.ContentSourcePath);
            var imageFilesToProcess = FileHelpers
                .GetImageFilesToProcess(allSourceFiles, dbReplacementTagValues);
            var markdownFilesToProcess = FileHelpers
                .GetMarkdownFiles(allSourceFiles);

            var filesToCopyAsIs = allSourceFiles
                .Where(f => !imageFilesToProcess.Contains(f))
                .Where(f => !markdownFilesToProcess.Contains(f));

            return new WebsiteStructure
            {
                TemplateFiles = templateFiles,
                AllSourceFiles = allSourceFiles,
                ImageFilesToProcess = imageFilesToProcess,
                MarkdownFilesToProcess = markdownFilesToProcess,
                FilesToCopyAsIs = filesToCopyAsIs,
                TemplateFileNameReplacementTagValues = templateFileNameReplacementTagValues,
                DbReplacementTagValues = dbReplacementTagValues
            };
        }

        public static void CopyAsIsFiles(string sourcePath, string buildOutputPath, IEnumerable<string> sourceFiles)
        {
            foreach (var sourceFile in sourceFiles)
            {
                var baseFilename = sourceFile.Replace(sourcePath, "");
                var outputFilename = Path.Combine(buildOutputPath, baseFilename);

                if (File.Exists(outputFilename))
                {
                    continue;
                }

                var fi = new FileInfo(outputFilename);
                var directory = fi?.Directory?.ToString();

                if ((directory != null) && (!Directory.Exists(directory)))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(sourceFile, outputFilename);
            }
        }
    }
}
