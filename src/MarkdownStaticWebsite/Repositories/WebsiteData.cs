using MarkdownStaticWebsite.Entities;
using MarkdownStaticWebsite.Modules;
using System.Data.SQLite;

namespace MarkdownStaticWebsite.Repositories
{
    public static class WebsiteData
    {
        /// <summary>
        /// Attempts to connect to the configuration database SQLite file.
        /// Creates the database if it does not exist.
        /// Attempts to create required tables in the database if they do not exist.
        /// </summary>
        public static void PrepareDatabase(string dbFile)
        {
            var isDbFileAlreadyThere = File.Exists(dbFile);
            using var connection = new SQLiteConnection($"Data Source={dbFile}");
            connection.Open();
            PrepareReplacementValuesTable(connection);
            PreparePagesTables(connection);
            PrepareBlogTables(connection);
            if (!isDbFileAlreadyThere)
            {
                Console.WriteLine($"Database just created: {dbFile}");
                // TODO: Add all required ReplacementValues items
                Console.WriteLine("In the ReplacementValues table: add an row with FieldName 'baseurl' and value like 'https://your.website'");
                throw new Exception($"Must initialize new database with a baseurl {dbFile}");
            }
        }

        private static void PrepareReplacementValuesTable(SQLiteConnection connection)
        {
            const string createTableSql = @"
CREATE TABLE IF NOT EXISTS ""ReplacementValues""
(
	""FieldName"" TEXT NOT NULL UNIQUE,
	""ReplacementValue""  TEXT NOT NULL
)
";

            CreateTableFromSql(createTableSql, connection);
        }

        private static void PreparePagesTables(SQLiteConnection connection)
        {
            const string createTableSql = @"
CREATE TABLE IF NOT EXISTS ""Pages""
(
	""PageId"" INTEGER PRIMARY KEY,
	""DateCreated"" TEXT NOT NULL,
	""Filename"" TEXT NOT NULL,
	""RelativePath"" TEXT NOT NULL,
	""RelativeUrl"" TEXT NOT NULL UNIQUE,
	""Url"" TEXT NOT NULL UNIQUE,
	""Title"" TEXT NOT NULL,
	""IncludeInNavigation"" INTEGER NOT NULL,
	""NavigationPosition"" INTEGER NULL
)
";

            CreateTableFromSql(createTableSql, connection);
        }

        private static void PrepareBlogTables(SQLiteConnection connection)
        {
            PrepareBlogPostsTable(connection);
            PrepareTagsTable(connection);
            PrepareBlogPostsTagsTable(connection);
            PrepareAuthorsTable(connection);
            PrepareBlogPostsAuthorsTable(connection);
        }

        private static void PrepareBlogPostsTable(SQLiteConnection connection)
        {
            const string createTableSql = @"
CREATE TABLE IF NOT EXISTS ""BlogPosts""
(
	""PostId"" INTEGER PRIMARY KEY,
	""DateCreated"" TEXT NOT NULL,
	""Filename"" TEXT NOT NULL,
	""RelativePath"" TEXT NOT NULL,
	""RelativeUrl"" TEXT NOT NULL UNIQUE,
	""Url"" TEXT NOT NULL UNIQUE,
	""Title"" TEXT NOT NULL
)
";

            CreateTableFromSql(createTableSql, connection);
        }

        private static void PrepareTagsTable(SQLiteConnection connection)
        {

            const string createTableSql = @"
CREATE TABLE IF NOT EXISTS ""Tags""
(
	""TagId"" INTEGER PRIMARY KEY,
	""DateCreated"" TEXT NOT NULL,
	""Tag"" TEXT NOT NULL UNIQUE
)
";

            CreateTableFromSql(createTableSql, connection);
        }

        private static void PrepareBlogPostsTagsTable(SQLiteConnection connection)
        {

            const string createTableSql = @"
CREATE TABLE IF NOT EXISTS ""BlogPostsTags""
(
	""PostId"" INTEGER NOT NULL,
	""TagId"" INTEGER NOT NULL
)
";

            CreateTableFromSql(createTableSql, connection);
        }

        private static void PrepareAuthorsTable(SQLiteConnection connection)
        {
            const string createTableSql = @"
CREATE TABLE IF NOT EXISTS ""Authors""
(
	""AuthorId"" INTEGER PRIMARY KEY,
	""DateCreated"" TEXT NOT NULL,
	""Author"" TEXT NOT NULL UNIQUE
)
";

            CreateTableFromSql(createTableSql, connection);
        }

        private static void PrepareBlogPostsAuthorsTable(SQLiteConnection connection)
        {
            const string createTableSql = @"
CREATE TABLE IF NOT EXISTS ""BlogPostsAuthors""
(
	""PostId"" INTEGER NOT NULL,
	""AuthorId"" INTEGER NOT NULL
)
";

            CreateTableFromSql(createTableSql, connection);
        }

        public static IDictionary<string, string> GetDatabaseReplacementTagValues(string dbFile)
        {
            //"Getting replacement values from database".Dump();

            var results = new Dictionary<string, string>();

            using (SQLiteConnection connection = new($"Data Source={dbFile}"))
            {
                connection.Open();

                const string replacementTagsQuery = @"
SELECT FieldName, ReplacementValue
FROM ReplacementValues
		";

                var command = connection.CreateCommand();
                command.CommandText = replacementTagsQuery;

                using var resultsReader = command.ExecuteReader();
                if (resultsReader.HasRows)
                {
                    while (resultsReader.Read())
                    {
                        var fieldName = resultsReader["FieldName"].ToString();
                        var replacementValue = resultsReader["ReplacementValue"].ToString();

                        if (string.IsNullOrEmpty(fieldName)) continue;
                        if (string.IsNullOrEmpty(replacementValue)) continue;

                        results.Add(fieldName, replacementValue);
                    }
                }

            }

            return results;
        }

        public static int? GetBlogPostIdFromUrl(string url, SQLiteConnection connection)
        {
            int? result = null;

            const string query = @"
SELECT PostId
FROM BlogPosts
WHERE Url = $Url
";

            var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("$Url", url);

            using (var resultsReader = command.ExecuteReader())
            {
                if (resultsReader.HasRows)
                {
                    while (resultsReader.Read())
                    {
                        if (int.TryParse(resultsReader["PostId"].ToString(), out int postId))
                        {
                            result = postId;
                        }
                    }
                }
            }

            return result;
        }

        public static int? GetAuthorIdByAuthor(string author, SQLiteConnection connection)
        {
            int? result = null;

            const string query = @"
SELECT AuthorId
FROM Authors
WHERE Author = $author
";

            var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("$author", author);

            using (var resultsReader = command.ExecuteReader())
            {
                if (resultsReader.HasRows)
                {
                    while (resultsReader.Read())
                    {
                        if (int.TryParse(resultsReader["AuthorId"].ToString(), out int authorId))
                        {
                            result = authorId;
                        }
                    }
                }
            }

            return result;
        }

        public static bool GetAreBlogPostIdAuthorIdLinked(int postId, int authorId, SQLiteConnection connection)
        {
            var result = false;

            const string query = @"
SELECT PostId, AuthorId
FROM BlogPostsAuthors
WHERE PostId = $postId
	AND AuthorId = $authorId
";

            var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("$postId", postId);
            command.Parameters.AddWithValue("$authorId", authorId);

            using (var resultsReader = command.ExecuteReader())
            {
                result = resultsReader.HasRows;
            }

            return result;
        }

        public static IEnumerable<int> GetBlogPostLinkedTagIds(int postId, SQLiteConnection connection)
        {
            var results = new List<int>();

            const string query = @"
SELECT TagId
FROM BlogPostsTags
WHERE PostId = $PostId
";

            var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("$PostId", postId);

            using (var resultsReader = command.ExecuteReader())
            {
                if (resultsReader.HasRows)
                {
                    while (resultsReader.Read())
                    {
                        if (int.TryParse(resultsReader["TagId"].ToString(), out int tagId))
                        {
                            results.Add(tagId);
                        }
                    }
                }
            }

            return results;
        }

        public static IDictionary<string, int> GetTagIdsByTags(IEnumerable<string> tags, SQLiteConnection connection)
        {
            var results = new Dictionary<string, int>();

            // TODO: a better way to do this?

            var tagsList = new List<string>();
            for (var i = 0; i < tags.Count(); i++)
            {
                tagsList.Add($"$tag{i}");
            }

            var query = $@"
SELECT TagId, Tag
FROM Tags
WHERE Tag IN ({string.Join(",", tagsList)})
";

            var command = connection.CreateCommand();
            command.CommandText = query;
            for (var i = 0; i < tags.Count(); i++)
            {
                command.Parameters.AddWithValue($"$tag{i}", tags.ElementAt(i));
            }

            using (var resultsReader = command.ExecuteReader())
            {
                if (resultsReader.HasRows)
                {
                    while (resultsReader.Read())
                    {
                        if (int.TryParse(resultsReader["TagId"].ToString(), out int tagId))
                        {
                            var tag = resultsReader["Tag"].ToString();

                            if (string.IsNullOrEmpty(tag)) continue;
                            
                            results.Add(tag, tagId);
                        }
                    }
                }
            }

            return results;
        }

        public static string GetNavigationPagesListItemLinks(SQLiteConnection connection)
        {
            var query = @"
SELECT Title, RelativeUrl
FROM Pages
WHERE IncludeInNavigation = 1 AND NavigationPosition IS NOT NULL
ORDER BY NavigationPosition ASC
";

            var command = connection.CreateCommand();
            command.CommandText = query;

            var titleAndUrls = new List<TitleAndUrl>();

            using (var resultsReader = command.ExecuteReader())
            {
                if (resultsReader.HasRows)
                {
                    while (resultsReader.Read())
                    {
                        var title = resultsReader["Title"].ToString();
                        var url = resultsReader["RelativeUrl"].ToString();

                        if (string.IsNullOrEmpty(title)) continue;
                        if (string.IsNullOrEmpty(url)) continue;

                        titleAndUrls.Add(new TitleAndUrl
                        {
                            Title = title,
                            Url = url
                        });
                    }
                }
            }

            return HtmlHelpers.GetListItemLinks(titleAndUrls);
        }

        private static void CreateTableFromSql(string createTableSql, SQLiteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = createTableSql;
            command.ExecuteNonQuery();
        }
    }
}
