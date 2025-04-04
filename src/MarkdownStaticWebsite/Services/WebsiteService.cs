﻿
using MarkdownStaticWebsite.Entities;
using MarkdownStaticWebsite.Modules;
using MarkdownStaticWebsite.Repositories;
using System.Data.SQLite;
using System.Text;

namespace MarkdownStaticWebsite.Services
{
    public class WebsiteService
    {
        private static readonly string ConnectionString = $"Data Source={ConfigurationService.GetService().Configuration.DatabaseFile}";

        public WebsiteService() { }

        public static WebsiteStructure ProcessWebsiteFiles()
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
                .GetImageFilesToProcess(ConfigurationService.GetService().Configuration.ImageFilesToProcessPath);
            var markdownFilesToProcess = FileHelpers
                .GetMarkdownFiles(allSourceFiles);

            // TODO: files and folders to skip
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

                var destinationFileExists = File.Exists(outputFilename);
                var overwriteDetinationFile = destinationFileExists
                    && !FileHelpers.GetMD5Checksum(sourceFile).Equals(FileHelpers.GetMD5Checksum(outputFilename));

                if (destinationFileExists && !overwriteDetinationFile)
                {
                    continue;
                }

                var fi = new FileInfo(outputFilename);
                var directory = fi?.Directory?.ToString();

                if ((directory != null) && (!Directory.Exists(directory)))
                {
                    Directory.CreateDirectory(directory);
                }

                Console.WriteLine($"Copying file as-is: {sourceFile} .. {outputFilename}");
                File.Copy(sourceFile, outputFilename, overwriteDetinationFile);
            }
        }

        public static IEnumerable<MarkdownFile> ConvertMarkdownToHtml(WebsiteStructure websiteStructure)
        {
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
            return processedMarkdownFiles;
        }

        public static void RenderAndWriteMarkdownFiles(IEnumerable<MarkdownFile> processedMarkdownFiles)
        {
            var renderTimeReplacements = new Dictionary<string, string>();

            // Collect/store DB metadata
            using (var connection = new SQLiteConnection(ConnectionString))
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
        }

        public static void GenerateAndWriteRssFeedFile(IDictionary<string, string> dbReplacementTagValues)
        {
            var rssFileBuilder = new StringBuilder();
            rssFileBuilder.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8"" ?>");
            rssFileBuilder.AppendLine(@"<rss version=""2.0"">");
            rssFileBuilder.AppendLine("<channel>");

            rssFileBuilder.AppendLine($"\t<link>{dbReplacementTagValues["baseurl"]}</link>");
            rssFileBuilder.AppendLine($"\t<title>{dbReplacementTagValues["blog-title"]}</title>");
            rssFileBuilder.AppendLine($"\t<description>{dbReplacementTagValues["blog-description"]}</description>");
            rssFileBuilder.AppendLine($"\t<language>{dbReplacementTagValues["locale"]}</language>");

            // TODO: add pubDate (The publication date for the content in the channel.)
            // TODO: add lastBuildDate (The last time the content of the channel changed.)
            // TODO: order by date of posts

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                var rssItems = WebsiteData.GetRssItemsFromBlogPosts(connection);

                foreach (var rssItem in rssItems)
                {
                    rssFileBuilder.AppendLine("\t<item>");
                    // RSS specification requires <author> to be an e-mail address, and I don't like that restriction
                    rssFileBuilder.AppendLine($"\t\t<link>{rssItem.Link}</link>");
                    var title = string.IsNullOrEmpty(rssItem.Title) ? "(no title)" : System.Web.HttpUtility.HtmlEncode(rssItem.Title);
                    rssFileBuilder.AppendLine($"\t\t<title>{title}</title>");
                    rssFileBuilder.AppendLine($"\t\t<description>\n{System.Web.HttpUtility.HtmlEncode(rssItem.Description)}\n\t\t</description>");
                    // "R" is in RFC-1123; RSS requires RFC-822 but RFC-1123 should be compatible
                    // https://stackoverflow.com/a/554093
                    rssFileBuilder.AppendLine($"\t\t<pubDate>{rssItem.Date.AddHours(12).ToString("R")}</pubDate>");
                    rssFileBuilder.AppendLine("\t</item>");
                }
            }

            rssFileBuilder.AppendLine("</channel>");
            rssFileBuilder.AppendLine("</rss>");

            var path = Path.EndsInDirectorySeparator(ConfigurationService.GetService().Configuration.BuildSiteOutputPath)
                            ? ConfigurationService.GetService().Configuration.BuildSiteOutputPath
                            : $"{ConfigurationService.GetService().Configuration.BuildSiteOutputPath}{Path.DirectorySeparatorChar}";
            var rssFeedFile = $"{path}blog.rss";

            Console.WriteLine($"Writing sitemap: {rssFeedFile}");

            File.WriteAllText(rssFeedFile, rssFileBuilder.ToString());
        }

        public static void GenerateAndWriteSitemapXmlFile(IEnumerable<MarkdownFile> processedMarkdownFiles)
        {
            var sitemapXmlBuilder = new StringBuilder();
            sitemapXmlBuilder.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sitemapXmlBuilder.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

            foreach (var markdownFile in processedMarkdownFiles.Where(f => f.GetType() == typeof(Page)))
            {
                //Console.WriteLine(markdownFile.BaseHtmlFilename);
                var fileInfo = new FileInfo(markdownFile.SourceFilePath);
                var dateModified = fileInfo.LastWriteTime;

                sitemapXmlBuilder.AppendLine("\t<url>");
                sitemapXmlBuilder.AppendLine($"\t\t<loc>{markdownFile.Url}</loc>");
                sitemapXmlBuilder.AppendLine($"\t\t<lastmod>{dateModified.AddHours(12).ToString("o")}</lastmod>");
                sitemapXmlBuilder.AppendLine("\t</url>");
            }
            sitemapXmlBuilder.AppendLine("</urlset>");

            var path = Path.EndsInDirectorySeparator(ConfigurationService.GetService().Configuration.BuildSiteOutputPath)
                            ? ConfigurationService.GetService().Configuration.BuildSiteOutputPath
                            : $"{ConfigurationService.GetService().Configuration.BuildSiteOutputPath}{Path.DirectorySeparatorChar}";
            var sitemapFile = $"{path}sitemap.xml";

            Console.WriteLine($"Writing sitemap: {sitemapFile}");

            File.WriteAllText(sitemapFile, sitemapXmlBuilder.ToString());
        }
    }
}
