好的，你希望实现一个功能，读取SQL Server中的 `lookup` schema 中的所有表，并根据每个表的结构生成C#的POCO类，并且生成静态初始化代码来填充这些POCO类的数据。我们将按照以下步骤来实现这一目标：

1. 使用 `.NET 8` 和 `C#` 的 `SqlClient` 连接 SQL Server，并读取指定 `lookup` schema 下的所有表结构。
2. 根据表的结构动态生成C# POCO类。
3. 创建静态方法，初始化并填充这些类的数据。

### 1. 创建一个 Console 应用程序

首先，创建一个 Console 应用程序，并安装必要的 NuGet 包：
- `System.Data.SqlClient`

在项目的 `csproj` 文件中，确保添加对 `System.Data.SqlClient` 的依赖：
```xml
<PackageReference Include="System.Data.SqlClient" Version="4.8.1" />
```

### 2. 创建数据库连接和表结构读取逻辑

接下来，创建一个静态类，负责从 SQL Server 中读取 `lookup` schema 下的所有表，并根据表的列生成对应的 C# POCO 类。

```csharp
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

class Program
{
    static void Main(string[] args)
    {
        string connectionString = "YourConnectionStringHere";
        var lookupTables = GetLookupTables(connectionString);

        foreach (var table in lookupTables)
        {
            Console.WriteLine(table);
            Console.WriteLine();
        }
    }

    public static List<string> GetLookupTables(string connectionString)
    {
        List<string> tables = new List<string>();

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'lookup'";
            SqlCommand command = new SqlCommand(query, connection);

            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                string tableName = reader.GetString(0);
                tables.Add(tableName);

                // 生成POCO类
                GeneratePocoClass(connection, tableName);
            }
        }

        return tables;
    }

    public static void GeneratePocoClass(SqlConnection connection, string tableName)
    {
        string query = $@"
            SELECT COLUMN_NAME, DATA_TYPE 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_SCHEMA = 'lookup' AND TABLE_NAME = '{tableName}'";

        SqlCommand command = new SqlCommand(query, connection);
        SqlDataReader reader = command.ExecuteReader();

        // 构建POCO类
        var className = "Lookup" + tableName.Substring(tableName.LastIndexOf('.') + 1);
        var classCode = $"public class {className}\n{{\n";

        List<string> properties = new List<string>();

        while (reader.Read())
        {
            string columnName = reader.GetString(0);
            string dataType = reader.GetString(1);

            string propertyType = ConvertSqlTypeToCSharp(dataType);
            properties.Add($"    public {propertyType} {columnName} {{ get; set; }}");
        }

        classCode += string.Join("\n", properties);
        classCode += "\n}";

        Console.WriteLine(classCode);

        // 生成静态初始化方法
        GenerateStaticMethod(className, tableName);
    }

    public static void GenerateStaticMethod(string className, string tableName)
    {
        string methodName = "Get" + className;
        string methodCode = $@"
public static List<{className}> {methodName}()
{{
    if (_lookup == null)
    {{
        _lookup = new List<{className}>();
        // Here we simulate the data loading
        _lookup.Add(new {className}() {{ code = 1, description = \"Sample\", isActive = true, comments = \"Sample comment\" }});
        // Add more data here as needed
    }}

    return _lookup;
}}";

        Console.WriteLine(methodCode);
    }

    public static string ConvertSqlTypeToCSharp(string sqlType)
    {
        // Convert SQL types to C# types
        return sqlType switch
        {
            "int" => "int",
            "varchar" => "string",
            "nvarchar" => "string",
            "bit" => "bool",
            "datetime" => "DateTime",
            "decimal" => "decimal",
            _ => "string",
        };
    }

    private static List<LookupCountry> _lookup;

    public class LookupCountry
    {
        public int Code { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string Comments { get; set; }
    }
}
```

### 3. 解释代码

- `GetLookupTables`: 这个方法从 `INFORMATION_SCHEMA.TABLES` 中获取所有属于 `lookup` schema 的表名。
- `GeneratePocoClass`: 这个方法通过查询 `INFORMATION_SCHEMA.COLUMNS` 获取表的所有列及其数据类型，然后基于此生成对应的 C# POCO 类代码。
- `GenerateStaticMethod`: 生成一个静态方法 `Get[ClassName]`，它会初始化一个列表并填充一些示例数据（在实际使用中，你可以从数据库中读取数据填充）。
- `ConvertSqlTypeToCSharp`: 这个方法将 SQL 类型转换为相应的 C# 类型。
- 在 `Main` 方法中，我们执行了 `GetLookupTables` 方法，它会获取所有 `lookup` schema 下的表，并为每个表生成 POCO 类代码和静态初始化代码。

### 4. 生成的 C# 代码示例

假设表 `lookup.country` 的结构如下：
```
code int
description varchar
isActive bit
comments varchar
```

生成的 `LookupCountry` 类代码会是：
```csharp
public class LookupCountry
{
    public int Code { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public string Comments { get; set; }
}

public static List<LookupCountry> GetLookupCountry()
{
    if (_lookup == null)
    {
        _lookup = new List<LookupCountry>();
        _lookup.Add(new LookupCountry() { Code = 1, Description = "Sample", IsActive = true, Comments = "Sample comment" });
        // Add more data here as needed
    }

    return _lookup;
}
```

### 5. 运行应用程序

1. 确保数据库连接字符串正确。
2. 运行程序，它会查询 `lookup` schema 下的所有表，并为每个表生成相应的 C# POCO 类和静态初始化代码。

### 总结

此解决方案通过反射数据库表结构来动态生成C# POCO类和静态方法，你可以根据需要进一步优化，例如处理更多数据类型或通过查询数据库获取实际数据填充列表。
