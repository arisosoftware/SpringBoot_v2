Yes! Your approach makes sense:  

1. **Use `SqlMetal.exe`** to generate an **XML (.dbml) file**.  
2. **Parse the `.dbml` in C#** to extract tables, columns, relationships, and comments.  
3. **Apply PascalCase naming, handle `Nullable<T>`, FK navigation, etc.**  
4. **Generate `.cs` files** for EF Core or .NET 4.8 models.  

---

### **1️⃣ Generate `.dbml` File Using SqlMetal**
Run this command to generate a **database markup file**:  

```sh
sqlmetal /server:YourServer /database:YourDatabase /dbml:DatabaseSchema.dbml
```

---

### **2️⃣ Read `.dbml` in C# and Transform Models**
The `.dbml` file is **XML-based**, so we can **load and parse it using LINQ to XML** in C#:  

#### **🔹 Load and Parse `DatabaseSchema.dbml`**
```csharp
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

class Program
{
    static void Main()
    {
        string dbmlPath = "DatabaseSchema.dbml";
        if (!File.Exists(dbmlPath))
        {
            Console.WriteLine("DBML file not found!");
            return;
        }

        XDocument dbml = XDocument.Load(dbmlPath);

        var tables = dbml.Descendants("Table").Select(table => new
        {
            Schema = table.Attribute("Member")?.Value.Split('.')[0] ?? "dbo",
            TableName = table.Attribute("Member")?.Value.Split('.').Last(),
            Columns = table.Descendants("Column").Select(col => new
            {
                Name = col.Attribute("Member")?.Value,
                Type = col.Attribute("Type")?.Value,
                IsNullable = col.Attribute("Nullable")?.Value == "true",
                Comment = col.Attribute("Description")?.Value
            }).ToList(),
            ForeignKeys = table.Descendants("Association").Select(fk => new
            {
                FKName = fk.Attribute("Member")?.Value,
                RelatedTable = fk.Attribute("Type")?.Value
            }).ToList()
        });

        foreach (var table in tables)
        {
            Console.WriteLine($"Table: {table.Schema}.{table.TableName}");
            foreach (var col in table.Columns)
            {
                Console.WriteLine($"  - {col.Name} ({col.Type}) {(col.IsNullable ? "NULLABLE" : "NOT NULL")}");
                if (!string.IsNullOrEmpty(col.Comment))
                    Console.WriteLine($"    COMMENT: {col.Comment}");
            }

            foreach (var fk in table.ForeignKeys)
            {
                Console.WriteLine($"  FK: {fk.FKName} -> {fk.RelatedTable}");
            }
        }
    }
}
```
✅ **This extracts all table names, columns, comments, and FK relationships.**  

---

### **3️⃣ Generate C# Entity Models (EF Core Style)**
After extracting data from `.dbml`, we can **generate `.cs` files**:

#### **🔹 Generate PascalCase Entity Classes**
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

class EntityGenerator
{
    public static void GenerateEntityClasses(string dbmlPath, string outputDir)
    {
        if (!File.Exists(dbmlPath))
        {
            Console.WriteLine("DBML file not found!");
            return;
        }

        XDocument dbml = XDocument.Load(dbmlPath);

        var tables = dbml.Descendants("Table").Select(table => new
        {
            Schema = table.Attribute("Member")?.Value.Split('.')[0] ?? "dbo",
            TableName = table.Attribute("Member")?.Value.Split('.').Last(),
            PascalName = ConvertToPascalCase(table.Attribute("Member")?.Value.Split('.').Last()),
            Columns = table.Descendants("Column").Select(col => new
            {
                Name = ConvertToPascalCase(col.Attribute("Member")?.Value),
                Type = ConvertToCSharpType(col.Attribute("Type")?.Value, col.Attribute("Nullable")?.Value == "true"),
                IsNullable = col.Attribute("Nullable")?.Value == "true",
                Comment = col.Attribute("Description")?.Value
            }).ToList(),
            ForeignKeys = table.Descendants("Association").Select(fk => new
            {
                FKName = ConvertToPascalCase(fk.Attribute("Member")?.Value),
                RelatedTable = ConvertToPascalCase(fk.Attribute("Type")?.Value)
            }).ToList()
        });

        Directory.CreateDirectory(outputDir);

        foreach (var table in tables)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine($"namespace GeneratedModels");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {table.Schema}{table.PascalName}");
            sb.AppendLine("    {");

            // Generate Properties
            foreach (var col in table.Columns)
            {
                if (!string.IsNullOrEmpty(col.Comment))
                    sb.AppendLine($"        /// <summary> {col.Comment} </summary>");
                sb.AppendLine($"        public {col.Type} {col.Name} {{ get; set; }}");
            }

            // Generate Foreign Key Navigation Properties
            foreach (var fk in table.ForeignKeys)
            {
                sb.AppendLine($"        public virtual {fk.RelatedTable} {fk.FKName} {{ get; set; }}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            string filePath = Path.Combine(outputDir, $"{table.Schema}{table.PascalName}.cs");
            File.WriteAllText(filePath, sb.ToString());
            Console.WriteLine($"Generated: {filePath}");
        }
    }

    static string ConvertToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return string.Join("", input.Split('_').Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
    }

    static string ConvertToCSharpType(string sqlType, bool isNullable)
    {
        Dictionary<string, string> typeMap = new Dictionary<string, string>
        {
            { "int", "int" },
            { "bigint", "long" },
            { "smallint", "short" },
            { "tinyint", "byte" },
            { "bit", "bool" },
            { "decimal", "decimal" },
            { "float", "double" },
            { "real", "float" },
            { "datetime", "DateTime" },
            { "date", "DateTime" },
            { "varchar", "string" },
            { "nvarchar", "string" },
            { "text", "string" },
            { "ntext", "string" }
        };

        string csharpType = typeMap.ContainsKey(sqlType) ? typeMap[sqlType] : "object";
        return isNullable && csharpType != "string" ? $"{csharpType}?" : csharpType;
    }
}

// Run the Generator
class Program
{
    static void Main()
    {
        EntityGenerator.GenerateEntityClasses("DatabaseSchema.dbml", "GeneratedEntities");
    }
}
```

---

### **🎯 Output Example (for `credit.history` Table)**
```csharp
namespace GeneratedModels
{
    public class CreditHistory
    {
        /// <summary> ID of the credit history </summary>
        public int Id { get; set; }

        /// <summary> Date of transaction </summary>
        public DateTime? TransactionDate { get; set; }

        /// <summary> Amount credited </summary>
        public decimal Amount { get; set; }

        /// <summary> Related User </summary>
        public virtual User User { get; set; }
    }
}
```

---

### **✅ Advantages of This Approach**
✔ **Correct PascalCase Naming**  
✔ **Includes Table Schema (`CreditHistory` for `credit.history`)**  
✔ **Handles `Nullable<T>` Properly**  
✔ **Foreign Key Relationships as `public virtual` Properties**  
✔ **Reads SQL Column Comments and Adds to C# Code**  

---

### **🚀 Final Thoughts**
Yes! Using **SqlMetal + C# XML Parsing + Code Generation** is a **great** approach to get **clean, structured EF Core models**.  

Let me know if you need any tweaks! 🚀
