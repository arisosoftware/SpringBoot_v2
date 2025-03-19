using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Globalization;

class Program
{
    private static string connectionString = ConfigurationManager.ConnectionStrings["DatabaseConnection"].ConnectionString;

    static void Main(string[] args)
    {
        var mappingRules = GetMappingRules();
        string dtoFolder = GetDtoFolder();

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            // Query to get all tables and views
            string query = @"
                SELECT TABLE_NAME, TABLE_TYPE, TABLE_SCHEMA
                FROM INFORMATION_SCHEMA.TABLES
            ";

            SqlCommand command = new SqlCommand(query, connection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string tableName = reader["TABLE_NAME"].ToString();
                string tableType = reader["TABLE_TYPE"].ToString();
                string schemaName = reader["TABLE_SCHEMA"].ToString();

                if (tableType == "BASE TABLE" || tableType == "VIEW")
                {
                    List<ColumnInfo> columns = GetColumns(tableName, connection);
                    List<ForeignKeyInfo> foreignKeys = GetForeignKeys(tableName, connection);

                    string modelCode = GenerateCSharpModel(tableName, columns, foreignKeys);
                    string targetFolder = GetTargetFolder(schemaName, mappingRules);

                    if (!Directory.Exists(targetFolder))
                    {
                        Directory.CreateDirectory(targetFolder);
                    }

                    string modelFilePath = Path.Combine(targetFolder, $"{tableName}.cs");
                    File.WriteAllText(modelFilePath, modelCode);
                    Console.WriteLine($"Generated: {modelFilePath}");

                    string dtoFilePath = Path.Combine(dtoFolder, $"{tableName}Dto.cs");
                    string dtoCode = GenerateDtoClass(tableName, columns);
                    File.WriteAllText(dtoFilePath, dtoCode);
                    Console.WriteLine($"Generated DTO: {dtoFilePath}");
                }
            }

            reader.Close();
        }
    }

    private static Dictionary<string, string> GetMappingRules()
    {
        var mappingRules = new Dictionary<string, string>();
        var mappingSection = ConfigurationManager.GetSection("mappingRules") as System.Collections.Hashtable;

        if (mappingSection != null)
        {
            foreach (var key in mappingSection.Keys)
            {
                string schema = key.ToString();
                string targetFolder = mappingSection[key].ToString();
                mappingRules.Add(schema, targetFolder);
            }
        }

        return mappingRules;
    }

    private static string GetTargetFolder(string schemaName, Dictionary<string, string> mappingRules)
    {
        return mappingRules.ContainsKey(schemaName) ? mappingRules[schemaName] : Path.Combine("C:\\GeneratedModels", schemaName);
    }

    private static string GetDtoFolder()
    {
        string dtoFolder = ConfigurationManager.AppSettings["dtoFolder"];
        return dtoFolder ?? "C:\\GeneratedDTOs";
    }

    private static List<ColumnInfo> GetColumns(string tableName, SqlConnection connection)
    {
        List<ColumnInfo> columns = new List<ColumnInfo>();

        string columnQuery = $@"
            SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = '{tableName}'
        ";

        SqlCommand columnCommand = new SqlCommand(columnQuery, connection);
        SqlDataReader columnReader = columnCommand.ExecuteReader();

        while (columnReader.Read())
        {
            columns.Add(new ColumnInfo
            {
                ColumnName = ConvertToPascalCase(columnReader["COLUMN_NAME"].ToString()),
                DataType = columnReader["DATA_TYPE"].ToString(),
                IsNullable = columnReader["IS_NULLABLE"].ToString() == "YES"
            });
        }

        columnReader.Close();
        return columns;
    }

    private static List<ForeignKeyInfo> GetForeignKeys(string tableName, SqlConnection connection)
    {
        List<ForeignKeyInfo> foreignKeys = new List<ForeignKeyInfo>();

        string fkQuery = $@"
            SELECT 
                fk.name AS FK_Name, 
                tp.name AS TableName, 
                cp.name AS ColumnName,
                tr.name AS ReferencedTable
            FROM sys.foreign_keys AS fk
            INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
            INNER JOIN sys.tables AS tp ON fkc.parent_object_id = tp.object_id
            INNER JOIN sys.columns AS cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
            INNER JOIN sys.tables AS tr ON fkc.referenced_object_id = tr.object_id
            WHERE tp.name = '{tableName}'
        ";

        SqlCommand fkCommand = new SqlCommand(fkQuery, connection);
        SqlDataReader fkReader = fkCommand.ExecuteReader();

        while (fkReader.Read())
        {
            foreignKeys.Add(new ForeignKeyInfo
            {
                ColumnName = ConvertToPascalCase(fkReader["ColumnName"].ToString()),
                ReferencedTable = ConvertToPascalCase(fkReader["ReferencedTable"].ToString())
            });
        }

        fkReader.Close();
        return foreignKeys;
    }

    private static string GenerateCSharpModel(string tableName, List<ColumnInfo> columns, List<ForeignKeyInfo> foreignKeys)
    {
        StringBuilder classCode = new StringBuilder();
        classCode.AppendLine($"public class {ConvertToPascalCase(tableName)}");
        classCode.AppendLine("{");

        foreach (var column in columns)
        {
            string csharpType = MapSqlTypeToCSharp(column.DataType, column.IsNullable);
            classCode.AppendLine($"    public {csharpType} {column.ColumnName} {{ get; set; }}");
        }

        foreach (var fk in foreignKeys)
        {
            classCode.AppendLine($"    public virtual {fk.ReferencedTable} {fk.ColumnName} {{ get; set; }}");
        }

        classCode.AppendLine("}");
        return classCode.ToString();
    }

    private static string GenerateDtoClass(string tableName, List<ColumnInfo> columns)
    {
        StringBuilder dtoCode = new StringBuilder();
        dtoCode.AppendLine($"public class {ConvertToPascalCase(tableName)}Dto");
        dtoCode.AppendLine("{");

        foreach (var column in columns)
        {
            string csharpType = MapSqlTypeToCSharp(column.DataType, column.IsNullable);
            dtoCode.AppendLine($"    public {csharpType} {column.ColumnName} {{ get; set; }}");
        }

        dtoCode.AppendLine("}");
        return dtoCode.ToString();
    }

    private static string MapSqlTypeToCSharp(string sqlType, bool isNullable)
    {
        string csharpType;
        switch (sqlType)
        {
            case "int":
                csharpType = "int";
                break;
            case "varchar":
            case "nvarchar":
            case "text":
                csharpType = "string";
                break;
            case "datetime":
                csharpType = "DateTime";
                break;
            case "bit":
                csharpType = "bool";
                break;
            case "decimal":
            case "money":
            case "smallmoney":
                csharpType = "decimal";
                break;
            case "float":
                csharpType = "double";
                break;
            default:
                csharpType = "string";
                break;
        }
        return isNullable && csharpType != "string" ? $"{csharpType}?" : csharpType;
    }

    private static string ConvertToPascalCase(string input)
    {
        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(input.ToLower()).Replace("_", "");
    }
}

public class ColumnInfo
{
    public string ColumnName { get; set; }
    public string DataType { get; set; }
    public bool IsNullable { get; set; }
}

public class ForeignKeyInfo
{
    public string ColumnName { get; set; }
    public string ReferencedTable { get; set; }
}
