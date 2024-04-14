// See https://aka.ms/new-console-template for more information

using MarkdownStaticWebsite.Entities;
using MarkdownStaticWebsite.Modules;
using MarkdownStaticWebsite.Repositories;
using MarkdownStaticWebsite.Services;
using System.Data.SQLite;


// TODO: Generate help text on the console

// TODO: Pass configuration file in as a command line parameter
//Console.WriteLine(Environment.GetCommandLineArgs()[1]);
ConfigurationService.GetService(Environment.GetCommandLineArgs()[1]);
//ConfigurationService.GetConfigurationService("C:\\temp\\website_test.config");

// TODO: Validate configuration file
//Console.WriteLine(ConfigurationService.ToJson(ConfigurationService.GetService().Configuration).ToString());

// TODO: Refactor all of this out of the Program file... for now let's just get things working and keep iterating

StorageService.GetStorageService().PrepareDatabase();
var websiteStructure = WebsiteService.ProcessTemplates();
WebsiteService.CopyAsIsFiles(
    ConfigurationService.GetService().Configuration.ContentSourcePath,
    ConfigurationService.GetService().Configuration.BuildSiteOutputPath,
    websiteStructure.FilesToCopyAsIs);

// Convert markdown to html
var processedMarkdownFiles = new List<MarkdownFile>();
foreach (var markdownFileToProcess in websiteStructure.MarkdownFilesToProcess)
{
    processedMarkdownFiles.Add(
        MarkdownFileFactory.GetMarkdownFile(
            markdownFileToProcess,
            ConfigurationService.GetService().Configuration.ContentSourcePath,
            ConfigurationService.GetService().Configuration.BuildSiteOutputPath,
            websiteStructure.DbReplacementTagValues["baseurl"],
            websiteStructure.TemplateFileNameReplacementTagValues,
            websiteStructure.DbReplacementTagValues)
    );
}


var renderTimeReplacements = new Dictionary<string, string>();

// Collect/store DB metadata
using (var connection = new SQLiteConnection($"Data Source={ConfigurationService.GetService().Configuration.DatabaseFile}"))
{
    connection.Open();

    var upserts = processedMarkdownFiles.SelectMany(pmf => pmf.GetUpsertCommands(connection));
    foreach (var upsert in upserts)
    {
        upsert.ExecuteNonQuery();
    }

    var secondaryUpserts = processedMarkdownFiles.SelectMany(pmf => pmf.GetLinkingTableUpsertCommands(connection));
    foreach (var secondaryUpsert in secondaryUpserts)
    {
        secondaryUpsert.ExecuteNonQuery();
    }


    // TODO: Replace image references with enhancements

    // Navigation of pages
    renderTimeReplacements.Add("navigation-link-items", WebsiteData.GetNavigationPagesListItemLinks(connection));

    // TODO: Pagination within posts

    // TODO: Related content
    renderTimeReplacements.Add("related-items", HtmlHelpers.GetListItemLinks(new List<TitleAndUrl>() { new TitleAndUrl() { Title = "Title", Url = "/" } }));

    // TODO: Tags pages

    // TODO: Pagination of posts
    renderTimeReplacements.Add("pagination", "");
}

// Render everything
foreach (var markdownFile in processedMarkdownFiles)
{
    // TODO: get post file navigation links and metadata
    Console.WriteLine($"Writing html from markdown file: {markdownFile.BaseMarkdownFilename}");
    markdownFile.WriteOutputFile(renderTimeReplacements);
}
