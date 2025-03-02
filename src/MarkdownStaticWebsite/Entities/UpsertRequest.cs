using System.Data.SQLite;

namespace MarkdownStaticWebsite.Entities
{
    public class UpsertRequest
    {
        public required string TableName { get; set; }
        public string WhereQualifier { get; set; } = string.Empty;
        public string CollisionField { get; set; } = string.Empty;
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
                object? value;
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
}
