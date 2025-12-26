using System.Data;

namespace SKLib;

public interface IDbHelper
{
    string GetDbSchema(string tableSchema="");
    DataTable RunQuery(string query);
}
