using Microsoft.Data.Sqlite;
using System.Data;
using System.Text;

namespace SKLib
{
    public class SqliteDbHelper : IDbHelper
    {
        readonly string _dbConStr;
        public SqliteDbHelper(string dbConStr)
        {
            _dbConStr = dbConStr;
        }

        public string GetDbSchema(string _ = "")
        {
            using var connection = new SqliteConnection(_dbConStr);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT name 
                FROM sqlite_master 
                WHERE type ='table' AND name NOT LIKE 'sqlite_%';
            ";

            using var reader = command.ExecuteReader();
            List<string> tables = new();
            while (reader.Read())
            {
                string tableName = reader.GetString(0);
                tables.Add(tableName);
            }
            reader.Close();
            reader.DisposeAsync();
            StringBuilder sb = new();
            foreach (var tbl in tables)
            {
                sb.Append($"{tbl} ");
                sb.Append(GetTableSchema(connection, tbl));
            }

            string schemaStr = sb.ToString();
            return schemaStr;
        }

        private static string GetTableSchema(SqliteConnection connection, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tableName});";
            StringBuilder sb = new("(");
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                string columnName = reader.GetString(1);
                string columnType = reader.GetString(2);
                sb.Append($"{columnName}:{columnType},");
            }
            string res = sb.ToString().TrimEnd(',');
            return res + ")\n";
        }

        public DataTable RunQuery(string query)
        {
            using var connection = new SqliteConnection(_dbConStr);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = query;
            using var reader = command.ExecuteReader();

            DataTable table = new();
            table.Load(reader);
      
            return table;
        }
    }
}
