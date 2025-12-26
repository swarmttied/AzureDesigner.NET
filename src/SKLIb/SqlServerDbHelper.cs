using Microsoft.Data.SqlClient;
using SKLib;
using System.Data;
using System.Text;

namespace SKLIb;

public class SqlServerDbHelper : IDbHelper
{
    readonly string _dbConStr;
    public SqlServerDbHelper(string dbConStr)
    {
        _dbConStr = dbConStr;
    }

    public string GetDbSchema(string tableSchema="dbo")
    {
        using var connection = new SqlConnection(_dbConStr);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @$"
                SELECT TABLE_NAME 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = '{tableSchema}';
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
            sb.Append($"SK.{tbl} ");
            sb.Append(GetTableSchema(connection, tbl));
        }

        string schemaStr = sb.ToString();
        return schemaStr;
    }

    private static string GetTableSchema(SqlConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
                SELECT COLUMN_NAME, DATA_TYPE 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @TableName;
            ";
        command.Parameters.AddWithValue("@TableName", tableName);
        StringBuilder sb = new("(");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            string columnName = reader.GetString(0);
            string columnType = reader.GetString(1);
            sb.Append($"{columnName}:{columnType}");
        }
        string res = sb.ToString().TrimEnd(',');
        return res + ")\n";
    }

    public DataTable RunQuery(string query)
    {
        using var connection = new SqlConnection(_dbConStr);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = query;

        using var adapter = new SqlDataAdapter(command);
        var dataTable = new DataTable();
        adapter.Fill(dataTable);

        return dataTable;
    }
}
