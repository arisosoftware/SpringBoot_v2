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
            List<TableInfo> tables = GetTables(conn);
            List<ForeignKeyInfo> foreignKeys = GetForeignKeys(conn);
            Dictionary<string, Dictionary<string, string>> columnComments = GetColumnComments(conn);

            foreach (TableInfo table in tables)
            {
                string classCode = GenerateEntityClass(conn, table, foreignKeys, columnComments);
                string filePath = Path.Combine(outputDirectory, table.Schema + "_" + table.Name + ".cs");
                File.WriteAllText(filePath, classCode, Encoding.UTF8);
                Console.WriteLine("Generated: " + filePath);
            }
        }
    }

    class TableInfo
    {
        public string Schema;
        public string Name;
    }

    static List<TableInfo> GetTables(SqlConnection conn)
    {
        List<TableInfo> tables = new List<TableInfo>();
        string query = "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'";

        using (SqlCommand cmd = new SqlCommand(query, conn))
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                tables.Add(new TableInfo
                {
                    Schema = reader.GetString(0),
                    Name = reader.GetString(1)
                });
            }
        }
        return tables;
    }

    class ForeignKeyInfo
    {
        public string TableSchema;
        public string Table;
        public string Column;
        public string RefSchema;
        public string RefTable;
        public string RefColumn;
    }

    static List<ForeignKeyInfo> GetForeignKeys(SqlConnection conn)
    {
        List<ForeignKeyInfo> foreignKeys = new List<ForeignKeyInfo>();
        string query = @"
            SELECT 
                s1.name AS TableSchema,
                tp.name AS TableName, 
                cp.name AS ColumnName, 
                s2.name AS RefSchema,
                tr.name AS RefTable, 
                cr.name AS RefColumn
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            INNER JOIN sys.tables tp ON tp.object_id = fkc.parent_object_id
            INNER JOIN sys.schemas s1 ON s1.schema_id = tp.schema_id
            INNER JOIN sys.columns cp ON cp.object_id = tp.object_id AND cp.column_id = fkc.parent_column_id
            INNER JOIN sys.tables tr ON tr.object_id = fkc.referenced_object_id
            INNER JOIN sys.schemas s2 ON s2.schema_id = tr.schema_id
            INNER JOIN sys.columns cr ON cr.object_id = tr.object_id AND cr.column_id = fkc.referenced_column_id";

        using (SqlCommand cmd = new SqlCommand(query, conn))
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                foreignKeys.Add(new ForeignKeyInfo
                {
                    TableSchema = reader.GetString(0),
                    Table = reader.GetString(1),
                    Column = reader.GetString(2),
                    RefSchema = reader.GetString(3),
                    RefTable = reader.GetString(4),
                    RefColumn = reader.GetString(5)
                });
            }
        }
        return foreignKeys;
    }

    static string GenerateEntityClass(SqlConnection conn, TableInfo table, 
                                      List<ForeignKeyInfo> foreignKeys, 
                                      Dictionary<string, Dictionary<string, string>> columnComments)
    {
        string className = ToPascalCase(table.Schema) + ToPascalCase(table.Name);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
        sb.AppendLine();
        sb.AppendLine($"[Table(\"{table.Name}\", Schema = \"{table.Schema}\")]");
        sb.AppendLine("public class " + className);
        sb.AppendLine("{");

        string query = "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '" + table.Schema + "' AND TABLE_NAME = '" + table.Name + "'";
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
                if (columnComments.ContainsKey(table.Name) && columnComments[table.Name].ContainsKey(columnName))
                {
                    string comment = columnComments[table.Name][columnName];
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

        // Handle foreign keys
        foreach (ForeignKeyInfo fk in foreignKeys.Where(f => f.Table == table.Name && f.TableSchema == table.Schema))
        {
            string propertyName = ToPascalCase(fk.RefSchema) + ToPascalCase(fk.RefTable);
            sb.AppendLine($"    public virtual {propertyName} {propertyName} {{ get; set; }}");
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
