
using MarkdownStaticWebsite.Modules;
using MarkdownStaticWebsite.Repositories;

namespace MarkdownStaticWebsite.Services
{
    public class WebsiteService
    {
        public WebsiteService() { }

        public static void ProcessTemplates()
        {
            var templateFiles = FileHelpers.GetTemplateFiles(
                ConfigurationService.GetConfigurationService().Configuration.TemplatePath);

            var templateFileNameReplacementTagValues = FileHelpers
                .GetTemplateFileNameReplacementTagValues(templateFiles);

            var dbReplacementTagValues = WebsiteData
                .GetDatabaseReplacementTagValues(ConfigurationService.GetConfigurationService().Configuration.DbFile);

            var allSourceFiles = FileHelpers
                .GetAllSourceFiles(ConfigurationService.GetConfigurationService().Configuration.SourcePath);
            var imageFilesToProcess = FileHelpers
                .GetImageFilesToProcess(allSourceFiles, dbReplacementTagValues);
            var markdownFilesToProcess = FileHelpers
                .GetMarkdownFiles(allSourceFiles);

            var filesToCopyAsIs = allSourceFiles
                .Where(f => !imageFilesToProcess.Contains(f))
                .Where(f => !markdownFilesToProcess.Contains(f));
        }
    }
}
