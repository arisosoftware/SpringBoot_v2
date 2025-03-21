Got it! I'll modify the code to be compatible with **.NET Framework 4.8** by:  

✅ **Removing modern syntax** (e.g., `switch expressions`, inline property initializers).  
✅ **Replacing `List<T>` initialization with `new List<T>()`** (since C# 9+ shorthand is not available).  
✅ **Using `foreach` instead of LINQ-heavy operations** for better compatibility.  

---

### **Updated Code for .NET Framework 4.8**
```csharp
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

class Program
{
    static void Main()
    {
        string connectionString = "your_connection_string_here"; // Replace with actual DB connection
        string outputDirectory = "GeneratedEntities"; // Output folder for .cs files

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            List<string> tables = GetTables(conn);
            List<ForeignKeyInfo> foreignKeys = GetForeignKeys(conn);
            Dictionary<string, Dictionary<string, string>> columnComments = GetColumnComments(conn);

            foreach (string table in tables)
            {
                string classCode = GenerateEntityClass(conn, table, foreignKeys, columnComments);
                string filePath = Path.Combine(outputDirectory, table + ".cs");
                File.WriteAllText(filePath, classCode, Encoding.UTF8);
                Console.WriteLine("Generated: " + filePath);
            }
        }
    }

    static List<string> GetTables(SqlConnection conn)
    {
        List<string> tables = new List<string>();
        string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'";

        using (SqlCommand cmd = new SqlCommand(query, conn))
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                tables.Add(reader.GetString(0));
            }
        }
        return tables;
    }

    class ForeignKeyInfo
    {
        public string Table;
        public string Column;
        public string RefTable;
        public string RefColumn;
    }

    static List<ForeignKeyInfo> GetForeignKeys(SqlConnection conn)
    {
        List<ForeignKeyInfo> foreignKeys = new List<ForeignKeyInfo>();
        string query = @"
            SELECT 
                tp.name AS TableName, 
                cp.name AS ColumnName, 
                tr.name AS RefTable, 
                cr.name AS RefColumn
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            INNER JOIN sys.tables tp ON tp.object_id = fkc.parent_object_id
            INNER JOIN sys.columns cp ON cp.object_id = tp.object_id AND cp.column_id = fkc.parent_column_id
            INNER JOIN sys.tables tr ON tr.object_id = fkc.referenced_object_id
            INNER JOIN sys.columns cr ON cr.object_id = tr.object_id AND cr.column_id = fkc.referenced_column_id";

        using (SqlCommand cmd = new SqlCommand(query, conn))
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                foreignKeys.Add(new ForeignKeyInfo
                {
                    Table = reader.GetString(0),
                    Column = reader.GetString(1),
                    RefTable = reader.GetString(2),
                    RefColumn = reader.GetString(3)
                });
            }
        }
        return foreignKeys;
    }

    static Dictionary<string, Dictionary<string, string>> GetColumnComments(SqlConnection conn)
    {
        Dictionary<string, Dictionary<string, string>> columnComments = new Dictionary<string, Dictionary<string, string>>();
        string query = @"
            SELECT 
                t.name AS TableName,
                c.name AS ColumnName,
                ep.value AS ColumnComment
            FROM sys.extended_properties ep
            INNER JOIN sys.tables t ON ep.major_id = t.object_id
            INNER JOIN sys.columns c ON ep.major_id = c.object_id AND ep.minor_id = c.column_id
            WHERE ep.name = 'MS_Description'";

        using (SqlCommand cmd = new SqlCommand(query, conn))
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string table = reader.GetString(0);
                string column = reader.GetString(1);
                string comment = reader.IsDBNull(2) ? "" : reader.GetString(2);

                if (!columnComments.ContainsKey(table))
                {
                    columnComments[table] = new Dictionary<string, string>();
                }
                columnComments[table][column] = comment;
            }
        }
        return columnComments;
    }

    static string GenerateEntityClass(SqlConnection conn, string table, 
                                      List<ForeignKeyInfo> foreignKeys, 
                                      Dictionary<string, Dictionary<string, string>> columnComments)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine("public class " + ToPascalCase(table));
        sb.AppendLine("{");

        string query = "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + table + "'";
        using (SqlCommand cmd = new SqlCommand(query, conn))
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                string columnName = reader.GetString(0);
                string dataType = reader.GetString(1);
                bool isNullable = reader.GetString(2) == "YES";
                string csharpType = MapToCSharpType(dataType, isNullable);

                // Fetch column comment if available
                if (columnComments.ContainsKey(table) && columnComments[table].ContainsKey(columnName))
                {
                    string comment = columnComments[table][columnName];
                    if (!string.IsNullOrWhiteSpace(comment))
                    {
                        sb.AppendLine("    /// <summary>");
                        sb.AppendLine("    /// " + comment);
                        sb.AppendLine("    /// </summary>");
                    }
                }

                sb.AppendLine("    public " + csharpType + " " + ToPascalCase(columnName) + " { get; set; }");
            }
        }

        // Handle multiple FK references to the same table
        foreach (ForeignKeyInfo fk in foreignKeys.Where(f => f.Table == table))
        {
            string propertyName = ToPascalCase(fk.RefTable);
            int count = foreignKeys.Count(f => f.Table == table && f.RefTable == fk.RefTable);
            if (count > 1)
            {
                propertyName += ToPascalCase(fk.Column);
            }
            sb.AppendLine("    public virtual " + ToPascalCase(fk.RefTable) + " " + propertyName + " { get; set; }");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    static string MapToCSharpType(string sqlType, bool isNullable)
    {
        string type;
        switch (sqlType.ToLower())
        {
            case "bigint": type = "long"; break;
            case "int": type = "int"; break;
            case "smallint": type = "short"; break;
            case "tinyint": type = "byte"; break;
            case "bit": type = "bool"; break;
            case "decimal":
            case "numeric": type = "decimal"; break;
            case "float": type = "double"; break;
            case "real": type = "float"; break;
            case "datetime":
            case "smalldatetime":
            case "date":
            case "time":
            case "datetime2": type = "DateTime"; break;
            case "char":
            case "varchar":
            case "text":
            case "nvarchar":
            case "ntext":
            case "nchar": type = "string"; break;
            case "uniqueidentifier": type = "Guid"; break;
            default: type = "object"; break;
        }

        if (isNullable && type != "string" && type != "object")
        {
            type = "Nullable<" + type + ">";
        }

        return type;
    }

    static string ToPascalCase(string input)
    {
        string[] words = input.Split(new char[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
        }
        return string.Join("", words);
    }
}
```

### **✔ Now Works with .NET 4.8**  
Would you like **DTO generation** next? 🚀
