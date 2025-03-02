// See https://aka.ms/new-console-template for more information

using MarkdownStaticWebsite.Services;

// TODO: Generate help text on the console

// TODO: Pass configuration file in as a command line parameter
#pragma warning disable CS8604 // Possible null reference argument.
ConfigurationService.GetService(args.ToList().FirstOrDefault());
#pragma warning restore CS8604 // Possible null reference argument.
//ConfigurationService.GetConfigurationService("C:\\temp\\website_test.config");

// TODO: Validate configuration file
//Console.WriteLine(ConfigurationService.ToJson(ConfigurationService.GetService().Configuration).ToString());

// Get the database ready
StorageService.GetStorageService().PrepareDatabase();

// Process templates to construct the structure of the website
var websiteStructure = WebsiteService.ProcessTemplates();

// Copy as-is files
WebsiteService.CopyAsIsFiles(
    ConfigurationService.GetService().Configuration.ContentSourcePath,
    ConfigurationService.GetService().Configuration.BuildSiteOutputPath,
    websiteStructure.FilesToCopyAsIs);

// Convert markdown files to html
var processedMarkdownFiles = WebsiteService.ConvertMarkdownToHtml(websiteStructure);
WebsiteService.RenderAndWriteMarkdownFiles(processedMarkdownFiles);

// Update RSS and Sitemap files
WebsiteService.GenerateAndWriteRssFeedFile(processedMarkdownFiles, websiteStructure.DbReplacementTagValues);
WebsiteService.GenerateAndWriteSitemapXmlFile(processedMarkdownFiles);
