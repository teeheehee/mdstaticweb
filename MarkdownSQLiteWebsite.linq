<Query Kind="Program">
  <NuGetReference>Markdig</NuGetReference>
  <NuGetReference>System.Data.SQLite</NuGetReference>
  <NuGetReference>YamlDotNet</NuGetReference>
  <Namespace>System.Data.SQLite</Namespace>
  <Namespace>YamlDotNet.Core</Namespace>
  <Namespace>YamlDotNet.Core.Events</Namespace>
  <Namespace>YamlDotNet.Serialization.NamingConventions</Namespace>
  <Namespace>Markdig</Namespace>
</Query>

void Main()
{
	const string DbFile = @"C:\dev\website\website.sqlite3";
	const string TemplatePath = @"C:\dev\website\templates";
	const string SourcePath = @"C:\dev\website\src";
	const string BuildOutputPath = @"C:\dev\website\build";
	
	PrepareDatabase(DbFile);

	var templateFiles = GetTemplateFiles(TemplatePath);
	
	var templateFileNameReplacementTagValues = GetTemplateFileNameReplacementTagValues(templateFiles);
	var dbReplacementTagValues = GetDatabaseReplacementTagValues(DbFile);

	var allSourceFiles = GetAllSourceFiles(SourcePath);
	var imageFilesToProcess = GetImageFilesToProcess(allSourceFiles, dbReplacementTagValues);
	var markdownFilesToProcess = GetMarkdownFiles(allSourceFiles);
	var filesToCopyAsIs = allSourceFiles
		.Where(f => !imageFilesToProcess.Contains(f))
		.Where(f => !markdownFilesToProcess.Contains(f));
	
	// TODO: Optimize, handle clean operation
	CopyAsIsFiles(SourcePath, BuildOutputPath, filesToCopyAsIs);

	// TODO: Process images


	// Convert markdown to html
	var processedMarkdownFiles = new List<MarkdownFile>();
	foreach (var markdownFileToProcess in markdownFilesToProcess)
	{
		processedMarkdownFiles.Add(
			MarkdownFileFactory.GetMarkdownFile(
				markdownFileToProcess,
				SourcePath,
				BuildOutputPath,
				dbReplacementTagValues["baseurl"],
				templateFileNameReplacementTagValues,
				dbReplacementTagValues)
		);
	}

	var renderTimeReplacements = new Dictionary<string, string>();

	// Collect/store DB metadata
	using (var connection = new SQLiteConnection($"Data Source={DbFile}"))
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
		renderTimeReplacements.Add("navigation-link-items", GetNavigationPagesListItemLinks(connection));

		// TODO: Pagination within posts

		// TODO: Related content
		renderTimeReplacements.Add("related-items", GetListItemLinks(new List<TitleAndUrl>() { new TitleAndUrl() { Title = "Title", Url = "/" } } ));

		// TODO: Tags pages

		// TODO: Pagination of posts
		renderTimeReplacements.Add("pagination", "");
	}

	// Render everything
	foreach (var markdownFile in processedMarkdownFiles)
	{
		markdownFile.WriteOutputFile(renderTimeReplacements);
	}

	// TODO: Generate search JSON file of tags and related files
	
	// TODO: Generate sitemaps for each rendered type of content,
	// and an index sitemap to point to all of those
	// https://en.wikipedia.org/wiki/Sitemaps
	// view-source:victoriousseo.com/sitemap_index.xml
	// view-source:victoriousseo.com/page-sitemap.xml
	// view-source:victoriousseo.com/post-sitemap.xml
	
	// TODO: Generate RSS XML feed
}

#region Helpers

public const string ReplacementTagRegexSearch = @"\${(.*?)}";
public static Regex ReplacementTagRegex = new Regex(ReplacementTagRegexSearch,
	RegexOptions.Compiled | RegexOptions.IgnoreCase);

public static string GetTagReplacementSearchString(string tag)
{
	// THIS ASSUMES TAG SYNTAX
	return $"${{{tag}}}";
}

public static bool ConfirmAllReplacementTagsHaveReplacementValues(
	IEnumerable<string> templateReplacementTags,
	IEnumerable<IDictionary<string, string>> replacementTags)
{
	var result = true;
	
	foreach (var tag in templateReplacementTags)
	{
		if (!replacementTags.Any(d => d.ContainsKey(tag)))
		{
			$">>> '{tag}' has no match".Dump();
			result = false;
		}
	}
	
	return result;
}

public static int GetNumberOfTabsPriorToSearchString(string searchString, string content)
{
	int result = 0;

	var start = content.IndexOf(GetTagReplacementSearchString(searchString)) - 1;
	while (start > 0 && content[start] == '\t')
	{
		result++;
		start--;
	}
	
	return result;
}

private static string IndentContent(int numberOfTabs, string content)
{
	var prefixTabs = GetTabs(numberOfTabs);
	var sb = new StringBuilder();

	foreach (var line in content.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.None))
	{
		sb.AppendLine($"{prefixTabs}{line}");
	}

	// Trimming the start because this will get Replace'd into an already indented position
	// Trimming the end to avoid introducing new NewLine's
	return sb.ToString().Trim();
}

private static string GetTabs(int numberOfTabs)
{
	var tabs = "";
	for (int i = 0; i < numberOfTabs; i++)
	{
		tabs += "\t";
	}
	return tabs;
}

private static bool ConvertYamlStringToBool(string value)
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

private static int? ConvertYamlStringToInt(string value)
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

private static string ApplyTagReplacements(string content, IDictionary<string, string> replacementTags)
{
	var results = content;
	foreach (var replacementTag in replacementTags.Keys)
	{
		var numberOfTabs = GetNumberOfTabsPriorToSearchString(replacementTag, results);
		var replacementText = IndentContent(numberOfTabs, replacementTags[replacementTag]);
		var searchString = GetTagReplacementSearchString(replacementTag);

		results = results.Replace(
			searchString,
			replacementText);
	}
	return results;
}

#endregion Helpers

#region Filesystem processing

public static IEnumerable<string> GetTemplateFiles(string path)
{
	"Getting all template files".Dump();
	return Directory.GetFiles(path, "*.html", SearchOption.AllDirectories).ToList();
}

public static IEnumerable<string> GetAllSourceFiles(string sourcePath)
{
	return Directory.EnumerateFiles(sourcePath, "*.*", SearchOption.AllDirectories);
}

public static IEnumerable<string> GetMarkdownFiles(IEnumerable<string> sourceFiles)
{
	return sourceFiles.Where(f => f.ToLowerInvariant().EndsWith(".md"));
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
		
	return results;
}

public static IEnumerable<string> GetReplacementTagsFromTemplateFile(string filePath)
{
	return GetReplacementTagsFromTemplateContents(File.ReadAllText(filePath));
}

public static IEnumerable<string> GetReplacementTagsFromTemplateContents(string contents)
{
	var results = new List<string>();
	
	var lines = contents.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.None);
	
	foreach (var line in lines)
	{
		var matches = ReplacementTagRegex.Matches(line);

		if (matches.Count > 0)
		{
			foreach (Match match in matches)
			{
				// get the matched search string that is inside the tag syntax
				var groups = match.Groups;
				// TODO: only run regex searches in non-codeblock elements (AngleSharp)
				// Omit finding things in a code example
				if (groups[1].Value == "(.*?)")
				{
					continue;
				}
				results.Add(groups[1].Value);
			}
		}
	}

	var uniqueResults = results.Distinct().ToList();

	//if (results.Any())
	//{
	//	$"Contents had {results.Count} replacement tags, {uniqueResults.Count} unique tags".Dump();
	//}
	//else
	//{
	//	"No replacement tags were found".Dump();
	//}

	return uniqueResults;
}

public static IDictionary<string, string> GetTemplateFileNameReplacementTagValues(IEnumerable<string> filePaths)
{
	"Getting template file names as replacement tags".Dump();

	var results = new Dictionary<string, string>();

	foreach (var filePath in filePaths)
	{
		var nameReplacementTag = new FileInfo(filePath).Name.Replace(".html", "");
		results.Add(nameReplacementTag, filePath);
	}

	return results;
}

#endregion Filesystem processing

#region Filesystem I/O

public static void CopyAsIsFiles(string sourcePath, string buildOutputPath, IEnumerable<string> sourceFiles)
{
	foreach (var sourceFile in sourceFiles)
	{
		var baseFilename = sourceFile.Replace(sourcePath, "");
		var outputFilename = buildOutputPath + baseFilename;
		
		if (File.Exists(outputFilename))
		{
			continue;
		}
		
		var fi = new FileInfo(outputFilename);
		var directory = fi.Directory.ToString();

		if (!Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}
		
		File.Copy(sourceFile, outputFilename);
	}
}

#endregion Filesystem I/O

#region Source file processing

private static string GetReplacementTagFromYamlTag(string yamlTag)
{
	var result = yamlTag.ToLowerInvariant();
	return Regex.Replace(result, @"\s", "-");
}

private static string GetYamlValue(string key, IDictionary<string, string> dictionary, string defaultValue = "")
{
	if (dictionary.ContainsKey(key))
	{
		return dictionary[key];
	}
	return defaultValue;
}

public static IDictionary<string, string> GetYamlFileContentReplacementTagValues(string content)
{
	var results = new Dictionary<string, string>();

	// https://markheath.net/post/markdown-html-yaml-front-matter
	var yamlDeserializer = new YamlDotNet.Serialization.DeserializerBuilder()
		.WithNamingConvention(CamelCaseNamingConvention.Instance)
		.Build();

	using (var input = new StringReader(content))
	{
		var parser = new Parser(input);
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

private static void EnsureSafeTemplateTree(
	string templateType,
	IDictionary<string, string> templateFileNameReplacementTagValues,
	IEnumerable<string> encounteredTemplateReplacementTags)
{
	var templateFile = templateFileNameReplacementTagValues[templateType];
	var replacementTags = GetReplacementTagsFromTemplateFile(templateFile);

	var templatesInTemplate = replacementTags
		.Where(rt => templateFileNameReplacementTagValues.ContainsKey(rt))
		.ToList();
	
	if (templatesInTemplate.Any(t => encounteredTemplateReplacementTags.Contains(t)))
	{
		throw new Exception($"Possible recursive loop in unpacking templates-within-templates: {templateFile}");
	}
	
	var updatedEncounteredTemplateReplacementTags = new List<string>();
	updatedEncounteredTemplateReplacementTags.AddRange(encounteredTemplateReplacementTags);
	updatedEncounteredTemplateReplacementTags.AddRange(templatesInTemplate);
	
	foreach (var template in templatesInTemplate)
	{
		EnsureSafeTemplateTree(
			template,
			templateFileNameReplacementTagValues,
			updatedEncounteredTemplateReplacementTags);
	}
}

private static string ConstructFullTemplate(
	int numberOfTabs,
	string templateType,
	IDictionary<string, string> templateFileNameReplacementTagValues)
{
	var templateContents = GetIndentedTemplateContents(numberOfTabs, templateFileNameReplacementTagValues[templateType]);

	var replacementTags = GetReplacementTagsFromTemplateContents(templateContents);
	var templatesInTemplate = replacementTags
		.Where(rt => templateFileNameReplacementTagValues.ContainsKey(rt))
		.ToList();

	foreach (var template in templatesInTemplate)
	{
		var fullNumberOfTabs = numberOfTabs + GetNumberOfTabsPriorToSearchString(template, templateContents);
		
		var subTemplateContents = ConstructFullTemplate(fullNumberOfTabs, template, templateFileNameReplacementTagValues);

		templateContents = templateContents.Replace(
			GetTagReplacementSearchString(template),
			subTemplateContents);
	}

	return templateContents;
}

private static string GetIndentedTemplateContents(int numberOfTabs, string templateFile)
{
	return IndentContent(numberOfTabs, File.ReadAllText(templateFile));
}

public static string GetListItemLinks(IEnumerable<TitleAndUrl> titleAndUrls)
{
	var sb = new StringBuilder();

	foreach (var titleAndUrl in titleAndUrls)
	{
		sb.AppendLine($"<li><a href=\"{titleAndUrl.Url}\">{titleAndUrl.Title}</a></li>");
	}

	return sb.ToString();
}

#endregion Source file processing

#region SQLite DB

public static void PrepareDatabase(string dbFile)
{
	"Making sure the database is available and has tables in it".Dump();
	
	using (var connection = new SQLiteConnection($"Data Source={dbFile}"))
	{
		connection.Open();
		PrepareReplacementValuesTable(connection);
		PreparePagesTables(connection);
		PrepareBlogTables(connection);
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
	"Getting replacement values from database".Dump();
	
	var results = new Dictionary<string, string>();
	
	using (var connection = new SQLiteConnection($"Data Source={dbFile}"))
	{
		connection.Open();
		
		const string replacementTagsQuery = @"
SELECT FieldName, ReplacementValue
FROM ReplacementValues
		";

		var command = connection.CreateCommand();
		command.CommandText = replacementTagsQuery;
		
		using (var resultsReader = command.ExecuteReader())
		{
			if (resultsReader.HasRows)
			{
				while (resultsReader.Read())
				{
					var fieldName = resultsReader["FieldName"].ToString();
					var replacementValue = resultsReader["ReplacementValue"].ToString();
					
					results.Add(fieldName, replacementValue);
				}
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
					results.Add(resultsReader["Tag"].ToString(), tagId);
				}
			}
		}
	}

	return results;
}

public static string GetNavigationPagesListItemLinks(SQLiteConnection connection)
{
	var query = @"
SELECT Title, Url
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
				titleAndUrls.Add(new TitleAndUrl
				{
					Title = resultsReader["Title"].ToString(),
					Url = resultsReader["Url"].ToString()
				});
			}
		}
	}
	
	return GetListItemLinks(titleAndUrls);
}

private static void CreateTableFromSql(string createTableSql, SQLiteConnection connection)
{
	var command = connection.CreateCommand();
	command.CommandText = createTableSql;
	command.ExecuteNonQuery();
}

#endregion SQLite DB

#region Helper classes

public class UpsertRequest
{
	public string TableName { get; set; }
	public string WhereQualifier { get; set; }
	public string CollisionField { get; set; }
	public IEnumerable<string> CollisionUpdateFields { get; set; } = new List<string>();
	public IDictionary<string, object> ColumnValuePairs { get; set; } = new Dictionary<string, object>();

	public SQLiteCommand GetUpsertCommand(SQLiteConnection connection)
	{
		// https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/parameters
		var columns = ColumnValuePairs.Keys.ToList();
		var columnSql = "(" + string.Join(",", columns) + ")";

		var values = columns.Select(c => $"${c}").ToList();
		var valuesSql = $"VALUES ({string.Join(",", values)})";

		var conflictResolution = CollisionUpdateFields.Any() ? "UPDATE SET" : "NOTHING";
		var conflictSql = string.IsNullOrEmpty(CollisionField)
			? ""
			: $"ON CONFLICT({CollisionField}) DO {conflictResolution}";

		var updates = CollisionUpdateFields.Select(uf => $"{uf} = excluded.{uf}");
		var updateSql = string.IsNullOrEmpty(CollisionField)
			? ""
			: string.Join($",{Environment.NewLine}\t", updates);

		// TODO: improve how the where qualifier is constructed
		var whereSql = string.IsNullOrEmpty(WhereQualifier) ? "" : $"WHERE {WhereQualifier}";

		var upsertSql = $@"
INSERT INTO {TableName}
	{columnSql}
	{valuesSql}
	{conflictSql}
		{updateSql}
	{whereSql}
";

		var upsertCmd = connection.CreateCommand();
		upsertCmd.CommandText = upsertSql;
		foreach (var column in columns)
		{
			// TODO: more cases, if needed
			object value;
			if (ColumnValuePairs[column] == null)
			{
				value = null;
			}
			else
			{
				switch (ColumnValuePairs[column].GetType().ToString())
				{
					case "System.Boolean":
						value = ((bool)ColumnValuePairs[column]) ? 1 : 0;
						break;
					case "System.Int32":
						value = (int)ColumnValuePairs[column];
						break;
					default:
						value = ColumnValuePairs[column].ToString();
						break;
				}
			}
			
			upsertCmd.Parameters.AddWithValue($"${column}", value);
		}

		return upsertCmd;
	}
}

public class MarkdownFileFactory
{
	public static MarkdownFile GetMarkdownFile(
		string sourceFilePath,
		string sourcePath,
		string buildOutputPath,
		string baseUrl,
		IDictionary<string, string> templateFileNameReplacementTagValues,
		IDictionary<string, string> dbReplacementTagValues)
	{
		var markdownContent = File.ReadAllText(sourceFilePath);
		var yamlFileContentReplacementTagValues = GetYamlFileContentReplacementTagValues(markdownContent);

		var templateType = yamlFileContentReplacementTagValues["type"].ToLower();

		switch (templateType)
		{
			case "post":
				return new Post(
					sourceFilePath,
					sourcePath,
					buildOutputPath,
					baseUrl,
					markdownContent,
					templateType,
					yamlFileContentReplacementTagValues,
					templateFileNameReplacementTagValues,
					dbReplacementTagValues
				);
			case "page":
			default:
				return new Page(
					sourceFilePath,
					sourcePath,
					buildOutputPath,
					baseUrl,
					markdownContent,
					templateType,
					yamlFileContentReplacementTagValues,
					templateFileNameReplacementTagValues,
					dbReplacementTagValues
				);
		}
	}
}

public abstract class MarkdownFile
{
	public string SourceFilePath { get; }
	public string SourcePath { get; }
	public string BuildOutputPath { get; }
	public string BuildOutputFilePath { get; }
	public string RelativeFilePath { get; }
	public string BaseMarkdownFilename { get; }
	public string BaseHtmlFilename { get; }
	public string BaseUrl { get; }
	public string RelativeUrlPath { get; }
	public string Url { get; }
	public string MarkdownContent { get; }
	public string HtmlContent { get { return RenderedHtmlContent; } }
	public string TemplateType { get; }
	public IDictionary<string, string> ReplacementTagValues { get; }
	
	protected IEnumerable<string> RendertimeTags { get; set; }
	protected string RenderedHtmlContent { get; set; }
	
	protected const string MarkdownContentTag = "markdown-content";
	protected const string PageUrlTag = "page-url";

	public MarkdownFile(
		string sourceFilePath,
		string sourcePath,
		string buildOutputPath,
		string baseUrl,
		string markdownContent,
		string templateType,
		IDictionary<string, string> yamlFileContentReplacementTagValues,
		IDictionary<string, string> templateFileNameReplacementTagValues,
		IDictionary<string, string> dbReplacementTagValues)
	{
		$"Preparing markdown file: {sourceFilePath}".Dump();

		SourceFilePath = sourceFilePath;
		SourcePath = new DirectoryInfo(sourcePath).ToString();
		BuildOutputPath = new DirectoryInfo(buildOutputPath).ToString();

		var sourceFile = new FileInfo(SourceFilePath);

		BaseMarkdownFilename = sourceFile.Name;
		BaseHtmlFilename = BaseMarkdownFilename.Replace(".md", ".html");
		
		RelativeFilePath = sourceFile.Directory.ToString().Replace(SourcePath, "") + @"\";
		
		BuildOutputFilePath = BuildOutputPath + RelativeFilePath + BaseHtmlFilename;
		BaseUrl = baseUrl.TrimEnd('/');
		RelativeUrlPath = RelativeFilePath.Replace(@"\", "/");
		Url = new Uri(BaseUrl + RelativeUrlPath + BaseHtmlFilename).AbsoluteUri;
		
		MarkdownContent = markdownContent;
		ReplacementTagValues = yamlFileContentReplacementTagValues;
		TemplateType = templateType;

		EnsureSafeTemplateTree(TemplateType, templateFileNameReplacementTagValues, new List<string>());

		var templateContents = ConstructFullTemplate(0, TemplateType, templateFileNameReplacementTagValues);

		// categorize available replacement tags
		// we replace in priority order from most to least significant

		var replacementTags = GetReplacementTagsFromTemplateContents(templateContents);

		var markdownReplacementTags = replacementTags.Where(rt => ReplacementTagValues.ContainsKey(rt));
		var dbReplacementTags = replacementTags.Where(rt => dbReplacementTagValues.ContainsKey(rt));
		
		var combinedReplacementTags = new Dictionary<string, string>();

		foreach (var markdownReplacementTag in markdownReplacementTags)
		{
			combinedReplacementTags.Add(markdownReplacementTag, ReplacementTagValues[markdownReplacementTag]);
		}

		foreach (var dbReplacementTag in dbReplacementTags)
		{
			if (!combinedReplacementTags.ContainsKey(dbReplacementTag))
			{
				combinedReplacementTags.Add(dbReplacementTag, dbReplacementTagValues[dbReplacementTag]);
			}
		}
		
		// apply tag replacements that are available, insert markdown rendered HTML
		
		templateContents = ApplyTagReplacements(templateContents, combinedReplacementTags);

		var numberOfMarkdownTabs = GetNumberOfTabsPriorToSearchString(MarkdownContentTag, templateContents);

		var markdownPipeline = new Markdig.MarkdownPipelineBuilder()
			.UseYamlFrontMatter() // strip YAML content prior to HTML rendering
			.UseAutoIdentifiers() // adds IDs to header tags with unique value
			.Build();

		// Fix line endings from Html process, and indent for applying to the template
		var markdownHtmlContent = IndentContent(
			numberOfMarkdownTabs,
			Markdig.Markdown.ToHtml(MarkdownContent, markdownPipeline).Replace("\n", Environment.NewLine));

		templateContents = templateContents
			.Replace(GetTagReplacementSearchString(MarkdownContentTag), markdownHtmlContent)
			.Replace(GetTagReplacementSearchString(PageUrlTag), Url);

		RenderedHtmlContent = templateContents;
	}

	public abstract IEnumerable<SQLiteCommand> GetUpsertCommands(SQLiteConnection connection);

	public abstract IEnumerable<SQLiteCommand> GetLinkingTableUpsertCommands(SQLiteConnection connection);

	protected void EnsureAllPrerenderTagsAreReplaced()
	{
		var remainingTags = GetReplacementTagsFromTemplateContents(HtmlContent);

		var unhandledTags = remainingTags.Where(rt => !RendertimeTags.Contains(rt)).ToList();
		if (unhandledTags.Any())
		{
			throw new Exception($"Did not replace all pre-renderable tags in file {SourceFilePath}: {string.Join(",", unhandledTags)}");
		}

	}
	
	protected void EnsureAllTagsAreReplaced()
	{
		var remainingTags = GetReplacementTagsFromTemplateContents(HtmlContent);
		
		if (remainingTags.Count() > 0)
		{
			throw new Exception($"Did not replace all tags in file {SourceFilePath}: {string.Join(",", remainingTags)}");
		}
	}

	public void WriteOutputFile(IDictionary<string, string> finalTagReplacements)
	{
		RenderedHtmlContent = ApplyTagReplacements(HtmlContent, ReplacementTagValues);
		RenderedHtmlContent = ApplyTagReplacements(HtmlContent, finalTagReplacements);
		
		// Check our work before committing to outputting the file
		EnsureAllTagsAreReplaced();
		
		if (!Directory.Exists(BuildOutputPath + RelativeFilePath))
		{
			Directory.CreateDirectory(BuildOutputPath + RelativeFilePath);
		}

		using (FileStream fs = File.Create(BuildOutputFilePath))
		{
			byte[] info = new UTF8Encoding(true).GetBytes(HtmlContent);
			fs.Write(info, 0, info.Length);
		}
	}
}

public class Page : MarkdownFile
{
	public Page(
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

		RendertimeTags = rendertimeTags;

		EnsureAllPrerenderTagsAreReplaced();
	}

	public override IEnumerable<SQLiteCommand> GetUpsertCommands(SQLiteConnection connection)
	{
		var results = new List<SQLiteCommand>();

		var showInNavigation = ConvertYamlStringToBool(GetYamlValue("show-in-navigation", ReplacementTagValues, "no"));
		var navigationPosition = ConvertYamlStringToInt(GetYamlValue("navigation-position", ReplacementTagValues));

		results.Add(new UpsertRequest
		{
			TableName = "Pages",
			CollisionField = "Url",
			ColumnValuePairs = new Dictionary<string, object>
					{
						{ "DateCreated", DateTime.Now.ToString("o") },
						{ "Filename", BaseMarkdownFilename },
						{ "RelativePath", RelativeUrlPath },
						{ "Url", Url },
						{ "Title", ReplacementTagValues["title"] },
						{ "IncludeInNavigation", showInNavigation },
						{ "NavigationPosition", navigationPosition }
					}
		}.GetUpsertCommand(connection));
		
		return results;
	}

	public override IEnumerable<SQLiteCommand> GetLinkingTableUpsertCommands(SQLiteConnection connection)
	{
		return new List<SQLiteCommand>();
	}
}

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
		var results = new List<SQLiteCommand>();

		results.Add(new UpsertRequest
		{
			TableName = "BlogPosts",
			CollisionField = "Url",
			ColumnValuePairs = new Dictionary<string, object>
					{
						{ "DateCreated", DateTime.Parse(ReplacementTagValues["date"]).ToString("o") },
						//{ "DateCreated", DateTime.Now.ToString("o") },
						{ "Filename", BaseMarkdownFilename },
						{ "RelativePath", RelativeUrlPath },
						{ "Url", Url },
						{ "Title", ReplacementTagValues["title"] }
					}
		}.GetUpsertCommand(connection));

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
		var postId = GetBlogPostIdFromUrl(Url, connection);

		if (!postId.HasValue)
		{
			return new List<SQLiteCommand>();
		}

		var results = new List<SQLiteCommand>();

		// Get Author Id
		var authorId = GetAuthorIdByAuthor(ReplacementTagValues["author"], connection);
		if (!GetAreBlogPostIdAuthorIdLinked(postId.Value, authorId.Value, connection))
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
		var tags = ReplacementTagValues["tags"].Split(',').Select(t => t.Trim());
		var tagsAndIds = GetTagIdsByTags(tags, connection);
		var linkedTags = GetBlogPostLinkedTagIds(postId.Value, connection);
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
}

public class TitleAndUrl
{
	public string Title { get; set; }
	public string Url { get; set; }
}

#endregion Helper classes
