using MarkdownStaticWebsite.Repositories;
using System.Configuration;
using System.Data.SQLite;
using System.Text.Json;

namespace MarkdownStaticWebsite.Entities
{
    public class Post : MarkdownFile
    {
        public Post(
            string sourceFilePath,
            string sourcePath,
            string buildOutputPath,
            string baseUrl,
            string markdownContent,
            string templateType,
            IDictionary<string, string> yamlFileContentReplacementTagValues,
            IDictionary<string, string> templateFileNameReplacementTagValues,
            IDictionary<string, string> dbReplacementTagValues)
            : base(
                sourceFilePath,
                sourcePath,
                buildOutputPath,
                baseUrl,
                markdownContent,
                templateType,
                yamlFileContentReplacementTagValues,
                templateFileNameReplacementTagValues,
                dbReplacementTagValues)
        {
            // Defer tag replacement for things that need to be prepared later and applied at render time
            var rendertimeTags = new List<string>();

            rendertimeTags.Add("navigation-link-items");
            rendertimeTags.Add("previous-post");
            rendertimeTags.Add("next-post");
            rendertimeTags.Add("reading-time");
            rendertimeTags.Add("pagination");
            rendertimeTags.Add("page-description");
            rendertimeTags.Add("related-items");
            ReplacementTagValues.Add("page-description", $"Blog post: {ReplacementTagValues["title"]}");

            RendertimeTags = rendertimeTags;

            EnsureAllPrerenderTagsAreReplaced();
        }

        public override IEnumerable<SQLiteCommand> GetUpsertCommands(SQLiteConnection connection)
        {
            var results = new List<SQLiteCommand>
            {
                new UpsertRequest
                {
                    TableName = "BlogPosts",
                    CollisionField = "Url",
                    ColumnValuePairs = new Dictionary<string, object>
                    {
                        { "DateCreated", DateTime.Parse(ReplacementTagValues["date"]).ToString("o") },
						//{ "DateCreated", DateTime.Now.ToString("o") },
						{ "Filename", BaseMarkdownFilename },
                        { "RelativePath", RelativeUrlPath },
                        { "RelativeUrl", RelativeUrl },
                        { "Url", Url },
                        { "Title", ReplacementTagValues["title"] },
                        { "MarkdownContent", MarkdownContent },
                        { "HtmlContent", MarkdownHtmlContent },
                        { "ReplacementTagValues", ConvertReplacementTagsToJson() }
                    },
                    CollisionUpdateFields = new List<string>
                    {
                        "DateCreated", "Title", "MarkdownContent", "HtmlContent", "ReplacementTagValues"
                    }
                }.GetUpsertCommand(connection)
            };

            if (ReplacementTagValues.ContainsKey("author"))
            {
                results.Add(new UpsertRequest
                {
                    TableName = "Authors",
                    CollisionField = "Author",
                    ColumnValuePairs = new Dictionary<string, object>
                        {
                            { "DateCreated", DateTime.Now.ToString("o") },
                            { "Author", ReplacementTagValues["author"] }
                        }
                }.GetUpsertCommand(connection));
            }

            if (ReplacementTagValues.ContainsKey("tags"))
            {
                var tags = ReplacementTagValues["tags"];
                foreach (var tag in tags.Split(','))
                {
                    results.Add(new UpsertRequest
                    {
                        TableName = "Tags",
                        CollisionField = "Tag",
                        ColumnValuePairs = new Dictionary<string, object>
                            {
                                { "DateCreated", DateTime.Now.ToString("o") },
                                { "Tag", tag.ToLower().Trim() }
                            }
                    }.GetUpsertCommand(connection));
                }
            }

            return results;
        }

        public override IEnumerable<SQLiteCommand> GetLinkingTableUpsertCommands(SQLiteConnection connection)
        {
            // Get Post Id
            var postId = WebsiteData.GetBlogPostIdFromUrl(Url, connection);

            if (!postId.HasValue) return new List<SQLiteCommand>();

            var results = new List<SQLiteCommand>();

            // Get Author Id
            var authorId = WebsiteData.GetAuthorIdByAuthor(ReplacementTagValues["author"], connection);
            if (!authorId.HasValue) return results;

            if (!WebsiteData.GetAreBlogPostIdAuthorIdLinked(postId.Value, authorId.Value, connection))
            {
                results.Add(new UpsertRequest
                {
                    TableName = "BlogPostsAuthors",
                    ColumnValuePairs = new Dictionary<string, object>
            {
                { "PostId", postId },
                { "AuthorId", authorId },
            }
                }.GetUpsertCommand(connection));
            }

            // Get Tag Ids
            var tags = ReplacementTagValues["tags"].Split(',').Select(t => t.ToLower().Trim()).ToList();
            var tagsAndIds = WebsiteData.GetTagIdsByTags(tags, connection);
            var linkedTags = WebsiteData.GetBlogPostLinkedTagIds(postId.Value, connection);
            var tagsToRemove = linkedTags.Where(lt => !tags.Contains(lt.ToString()));
            var tagsToAdd = tagsAndIds.Values.Where(t => !linkedTags.Contains(t));
            foreach (var tagToAdd in tagsToAdd)
            {
                results.Add(new UpsertRequest
                {
                    TableName = "BlogPostsTags",
                    ColumnValuePairs = new Dictionary<string, object>
                {
                    { "PostId", postId },
                    { "TagId", tagToAdd },
                }
                }.GetUpsertCommand(connection));
            }

            if (tagsToRemove.Any())
            {
                var removeTagsSql = @"
DELETE FROM ""BlogPostsTags""
WHERE PostId = $postId
	AND TagId IN ($tagIds)
";
                var removeTagsCmd = connection.CreateCommand();
                removeTagsCmd.CommandText = removeTagsSql;
                removeTagsCmd.Parameters.AddWithValue("$postId", postId);
                removeTagsCmd.Parameters.AddWithValue("$tagIds", string.Join(",", tagsToRemove));
                results.Add(removeTagsCmd);
            }

            return results;
        }
        
        private string ConvertReplacementTagsToJson()
        {

            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
            return JsonSerializer.Serialize(ReplacementTagValues, options);
        }
    }
}
