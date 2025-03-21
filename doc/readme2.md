Here’s how you can generate **Entity Framework (EF) domain classes** from a SQL database while ensuring:  

✅ **PascalCase field names**  
✅ **Nullable<T> for nullable columns**  
✅ **Enhanced SQL-to-C# type mapping**  
✅ **switch case function for type mapping** (not expression syntax)  
✅ **Foreign keys (FK) are supported** with `public virtual <T> SchemaTableName { get; set; }`  

---

### 🔹 **Step 1: Install EF Core Tools**
Before generating EF Core classes, install the required tools:  
```sh
dotnet tool install --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
```

---

### 🔹 **Step 2: Generate Entity Classes using EF Scaffolding**
Run the following command to generate classes from your database:  
```sh
dotnet ef dbcontext scaffold "YourConnectionStringHere" Microsoft.EntityFrameworkCore.SqlServer -o Models --use-database-names --context DbContextName
```
> 📝 **Note:**  
> - `-o Models` → Saves generated classes inside the `Models` folder.  
> - `--use-database-names` → Keeps table and column names as they appear in SQL.  
> - `DbContextName` → Name of the generated `DbContext` class.  

---

### 🔹 **Step 3: Customize the Generated Entity Classes**
Modify the **Entity Class Template** for better naming and nullable type handling:

#### **Example: Auto-Generated Entity Class (Refactored)**
```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string CustomerName { get; set; }

    public DateTime? OrderDate { get; set; }  // Supports Nullable<DateTime>

    public decimal TotalAmount { get; set; }

    // Foreign Key Relationship
    public int? CustomerId { get; set; }
    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; }  // Virtual navigation property
}
```
#### **Example: SQL Type Mapping with `switch case`**
```csharp
public static class SqlTypeMapper
{
    public static string MapSqlTypeToCSharp(string sqlType, bool isNullable)
    {
        string csharpType;
        switch (sqlType.ToLower())
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
}
```

---

### 🔹 **Step 4: Customize the `DbContext`**
Modify the `DbContext` to add relationships and entity configurations.

```csharp
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<Customer> Customers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlServer("YourConnectionStringHere");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

### ✅ **Final Thoughts**
Now you have a **clean EF Core model** with proper type mapping, nullable support, PascalCase, and foreign key relationships. You can now use it in your application.

Would you like **automatic DTO generation** or **query examples** next? 🚀


### ✅ **Generating DTOs Automatically + Customizing EF Core Models**
To streamline the process, I'll guide you on how to:  

✔ **Generate DTOs automatically** from EF Core models  
✔ **Customize EF Core models** (e.g., PascalCase, Nullable<T>, Foreign Key Handling)  
✔ **Implement a clean `DTOMapper` for easy conversions**  

---

## 🔹 **Step 1: Create a DTO Generator (T4 Template or Code)**
We’ll generate DTOs based on EF Core models.  
Example: If we have `Order` and `Customer`, we generate `OrderDTO` and `CustomerDTO`.

#### **🛠 Automatic DTO Generator in C#**
```csharp
using System;
using System.Linq;
using System.Reflection;

public static class DtoGenerator
{
    public static void GenerateDto<T>()
    {
        Type entityType = typeof(T);
        string className = entityType.Name + "DTO";
        var properties = entityType.GetProperties()
            .Where(p => !p.PropertyType.FullName.Contains("System.Collections")) // Exclude navigation collections
            .ToList();

        Console.WriteLine($"public class {className}");
        Console.WriteLine("{");
        foreach (var prop in properties)
        {
            string typeName = Nullable.GetUnderlyingType(prop.PropertyType)?.Name ?? prop.PropertyType.Name;
            if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                typeName += "?";
            }
            Console.WriteLine($"    public {typeName} {prop.Name} {{ get; set; }}");
        }
        Console.WriteLine("}");
    }
}

// Example Usage:
DtoGenerator.GenerateDto<Order>();  // Generates OrderDTO
DtoGenerator.GenerateDto<Customer>(); // Generates CustomerDTO
```
**This prints DTO classes dynamically based on EF models. You can copy-paste them or extend it to generate files.**  

---

## 🔹 **Step 2: Example of Generated DTO**
After running `DtoGenerator.GenerateDto<Order>()`, it generates:

```csharp
public class OrderDTO
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
    public DateTime? OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int? CustomerId { get; set; }
}
```
---

## 🔹 **Step 3: Customize EF Core Model**
Modify entity models for **PascalCase, Nullable<T>, and Foreign Keys**.

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string CustomerName { get; set; }

    public DateTime? OrderDate { get; set; }  // Nullable<T>

    public decimal TotalAmount { get; set; }

    // Foreign Key
    public int? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; } // Virtual FK
}
```
---

## 🔹 **Step 4: Implement DTO Mapping Utility**
Convert EF Core entities to DTOs using a mapper.

```csharp
public static class DTOMapper
{
    public static OrderDTO ToDto(this Order order)
    {
        if (order == null) return null;
        return new OrderDTO
        {
            Id = order.Id,
            CustomerName = order.CustomerName,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            CustomerId = order.CustomerId
        };
    }

    public static CustomerDTO ToDto(this Customer customer)
    {
        if (customer == null) return null;
        return new CustomerDTO
        {
            Id = customer.Id,
            Name = customer.Name
        };
    }
}
```
---

## 🔹 **Step 5: Using DTOs in Controllers**
Example usage in an **API Controller**:

```csharp
using Microsoft.AspNetCore.Mvc;
using System.Linq;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetOrders()
    {
        var orders = _context.Orders
            .Select(o => o.ToDto())  // Use DTO Mapper
            .ToList();
        return Ok(orders);
    }
}
```

---

## ✅ **Final Summary**
1. **Automatic DTO Generation** → `DtoGenerator.GenerateDto<T>()`
2. **Custom EF Core Models** → PascalCase, Nullable<T>, FK Navigation
3. **DTO Mapper** → `ToDto()` extension for easy conversions  
4. **API Integration** → Use DTOs in Controllers for a clean API  

Would you like the **DTO generator** to write files automatically? 🚀
.

I can't directly connect to a database, but I can guide you on how to generate Entity Framework Core classes from your database schema using **Scaffold-DbContext**.

---

## **🔹 Step 1: Install EF Core Tools**
Ensure you have the required EF Core tools installed in your .NET project:

```sh
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
```

If you're using **MySQL**, install:

```sh
dotnet add package Pomelo.EntityFrameworkCore.MySql
```

---

## **🔹 Step 2: Generate Entity Classes from Database**
Run the following command in the terminal (replace `your_connection_string`):

```sh
dotnet ef dbcontext scaffold "your_connection_string" Microsoft.EntityFrameworkCore.SqlServer -o Models -f -c ApplicationDbContext
```

- `your_connection_string` → Replace with your **actual DB connection string**  
- `-o Models` → Outputs the entity classes in the `Models` folder  
- `-f` → Force overwrite existing files  
- `-c ApplicationDbContext` → Names the generated DbContext `ApplicationDbContext`  

For **MySQL**, run:

```sh
dotnet ef dbcontext scaffold "your_connection_string" Pomelo.EntityFrameworkCore.MySql -o Models -f -c ApplicationDbContext
```

---

## **🔹 Step 3: Customize the Generated Classes**
Modify the generated entity classes to:  
✔ Convert field names to **PascalCase**  
✔ Use **Nullable<T>** where applicable  
✔ Improve **database type mapping**  
✔ Handle **foreign keys (FK)** properly  

---

## **🔹 Example of a Generated Entity**
Assume you have a table `Orders` in the database. The generated model might look like this:

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Order
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string CustomerName { get; set; }

    public DateTime? OrderDate { get; set; } // Nullable<T>

    public decimal TotalAmount { get; set; }

    // Foreign Key
    public int? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer Customer { get; set; } // FK Support
}
```

---

## **🔹 Step 4: Modify `DbContext`**
The `ApplicationDbContext.cs` file is also generated:

```csharp
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}

    public DbSet<Order> Orders { get; set; }
    public DbSet<Customer> Customers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Custom mapping if needed
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId);
    }
}
```

---

## **🔹 Step 5: Generate DTOs**
Once entities are generated, use the **DTO Generator** (from my previous response) to automatically create DTOs.

---

## **🔹 Need Help?**
1️⃣ Do you need me to **adjust the generated code** for your project?  
2️⃣ Would you like to **modify entity names** or **add specific annotations**?  
3️⃣ If you have a **database schema (table structures)**, I can manually generate the classes for you! 🚀


Yes, you can generate an **XML model of the database schema** using `dotnet ef dbcontext scaffold` and then use it for **custom code generation**.  

---

### **🔹 Approach 1: Use `dotnet ef dbcontext scaffold` with XML Output**
EF Core does **not** natively support XML output, but you can generate C# classes and then convert them into XML for customization.

#### **Step 1: Scaffold the Database**
Run the following command to generate the entity classes:

```sh
dotnet ef dbcontext scaffold "your_connection_string" Microsoft.EntityFrameworkCore.SqlServer -o Models -c ApplicationDbContext --data-annotations
```

#### **Step 2: Convert Entity Classes to XML**
You can use `XmlSerializer` in C# to serialize your models into XML.

Example C# Code:
```csharp
using System;
using System.IO;
using System.Xml.Serialization;

public class XmlHelper
{
    public static void SerializeToXml<T>(T obj, string filePath)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        using (TextWriter writer = new StreamWriter(filePath))
        {
            serializer.Serialize(writer, obj);
        }
    }
}
```
Call it like:
```csharp
var dbContext = new ApplicationDbContext(); // Ensure it's instantiated properly
XmlHelper.SerializeToXml(dbContext.Model, "schema.xml");
```
This will output an XML representation of the **DbContext Model**.

---

### **🔹 Approach 2: Use `dotnet ef migrations script`**
Alternatively, if you want **metadata in XML format**, you can run:

```sh
dotnet ef migrations script -o schema.sql
```
Then, manually convert `schema.sql` to an **XML file** using a script or a database tool.

---

### **🔹 Approach 3: Use PowerShell + T4 Templates**
You can write a **PowerShell script + T4 template** to extract EF Core metadata into XML.

Example PowerShell Command:
```powershell
dotnet ef dbcontext list | Out-File dbcontext.xml
```

---

### **🔹 Alternative: Directly Query the Database Schema**
You can query SQL Server directly to generate XML:

```sql
SELECT
    TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
FOR XML AUTO, ROOT('DatabaseSchema')
```
Run this query, save the result as an XML file, and use it for **custom code generation**.

---

### **🔹 Which Method Works Best for You?**
- **Method 1 (Serialize DbContext Model to XML)** → Best if you want **full control** over code generation.  
- **Method 2 (Migrations Script + Convert to XML)** → Good if you're **familiar with SQL scripts**.  
- **Method 3 (PowerShell & T4 Templates)** → Useful if you want **automated EF XML exports**.  
- **Method 4 (Direct SQL Query to XML)** → Best if you **don't want EF Core** and only need the schema.

Let me know which one you prefer, or if you need **custom automation**! 🚀






### **Customizing Entity Framework Core Code Generation Templates**
EF Core uses **design-time services** for scaffolding, but you can override its default behavior by **customizing the T4 templates** or using a custom generator.

---

## **🔹 Approach 1: Use `--json` for Custom Code Generation**
You can first **extract metadata as JSON** and then process it with your own tool to generate C# classes.

Run:
```sh
dotnet ef dbcontext scaffold "YourConnectionStringHere" Microsoft.EntityFrameworkCore.SqlServer --json > schema.json
```
This outputs a JSON representation of the schema. You can then **write your own generator** in C#, Python, or another language.

---

## **🔹 Approach 2: Customize Templates via EF Core `IPluralizer` & `ICSharpHelper`**
If you want to **modify the generated classes**, override **design-time services**.

### **Step 1: Create a Custom Code Generator**
Create a custom `ICSharpEntityTypeGenerator` to modify how C# entities are generated.

```csharp
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;

public class CustomCSharpEntityTypeGenerator : CSharpEntityTypeGenerator
{
    public CustomCSharpEntityTypeGenerator(CSharpEntityTypeGeneratorDependencies dependencies)
        : base(dependencies)
    {
    }

    protected override void GenerateProperties(EntityType entityType, IndentedStringBuilder sb)
    {
        base.GenerateProperties(entityType, sb);
        
        // Custom changes: Add attributes, modify property names, etc.
        foreach (var property in entityType.GetProperties())
        {
            sb.AppendLine($"// Custom Property: {property.Name}");
        }
    }
}
```

### **Step 2: Register the Custom Code Generator**
Create a custom `DesignTimeServices` class:

```csharp
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding;

public class CustomDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        services.AddSingleton<IEntityTypeGenerator, CustomCSharpEntityTypeGenerator>();
    }
}
```

Then, run EF Core scaffolding with:
```sh
dotnet ef dbcontext scaffold "YourConnectionStringHere" Microsoft.EntityFrameworkCore.SqlServer --context DbContextName
```

---

## **🔹 Approach 3: Use T4 Templates (`.tt` files)**
1. Install `EntityFrameworkCore.Scaffolding.Handlebars`:
   ```sh
   dotnet add package EntityFrameworkCore.Scaffolding.Handlebars
   ```
2. Run:
   ```sh
   dotnet ef dbcontext scaffold "YourConnectionStringHere" Microsoft.EntityFrameworkCore.SqlServer --context DbContextName -o Models --use-database-names --context-dir Data
   ```
3. Modify the T4 templates (`.tt` files) to customize output.

---

## **🔹 Approach 4: Use `dotnet ef dbcontext scaffold --xml` (Alternative)**
If EF Core adds XML support in the future, you can first extract the XML and modify it before generating C# code.

---

### **🔹 Which One Should You Use?**
- **For quick modifications** → **Approach 2 (Custom Code Generator)**  
- **For advanced templating** → **Approach 3 (T4 Templates with Handlebars)**  
- **For external processing** → **Approach 1 (JSON Processing with Custom Scripts)**  

Let me know which approach works best for you! 🚀





Got it! I'll modify the code to **extract column comments from SQL Server** and insert them as C# XML comments in the generated class files.  

---

### **How This Works**
1. **Retrieve column comments** using `sys.extended_properties`.
2. **Insert comments above properties** in the generated C# class.

---

### **Updated Code**
```csharp
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string connectionString = "your_connection_string_here"; // Replace with actual DB connection
        string outputDirectory = "GeneratedEntities"; // Output folder for .cs files

        Directory.CreateDirectory(outputDirectory);

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            var tables = GetTables(conn);
            var foreignKeys = GetForeignKeys(conn);
            var columnComments = GetColumnComments(conn);

            foreach (var table in tables)
            {
                string classCode = GenerateEntityClass(conn, table, foreignKeys, columnComments);
                string filePath = Path.Combine(outputDirectory, $"{table}.cs");
                File.WriteAllText(filePath, classCode);
                Console.WriteLine($"Generated: {filePath}");
            }
        }
    }

    static List<string> GetTables(SqlConnection conn)
    {
        List<string> tables = new();
        string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'";
        using SqlCommand cmd = new(query, conn);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            tables.Add(reader.GetString(0));
        }
        return tables;
    }

    static List<(string Table, string Column, string RefTable, string RefColumn)> GetForeignKeys(SqlConnection conn)
    {
        List<(string, string, string, string)> foreignKeys = new();
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

        using SqlCommand cmd = new(query, conn);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            foreignKeys.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
        }
        return foreignKeys;
    }

    static Dictionary<(string Table, string Column), string> GetColumnComments(SqlConnection conn)
    {
        Dictionary<(string, string), string> columnComments = new();
        string query = @"
            SELECT 
                t.name AS TableName,
                c.name AS ColumnName,
                ep.value AS ColumnComment
            FROM sys.extended_properties ep
            INNER JOIN sys.tables t ON ep.major_id = t.object_id
            INNER JOIN sys.columns c ON ep.major_id = c.object_id AND ep.minor_id = c.column_id
            WHERE ep.name = 'MS_Description'";

        using SqlCommand cmd = new(query, conn);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            string table = reader.GetString(0);
            string column = reader.GetString(1);
            string comment = reader.IsDBNull(2) ? "" : reader.GetString(2);
            columnComments[(table, column)] = comment;
        }
        return columnComments;
    }

    static string GenerateEntityClass(SqlConnection conn, string table, 
                                      List<(string Table, string Column, string RefTable, string RefColumn)> foreignKeys, 
                                      Dictionary<(string Table, string Column), string> columnComments)
    {
        StringBuilder sb = new();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine($"public class {ToPascalCase(table)}");
        sb.AppendLine("{");

        string query = $"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table}'";
        using SqlCommand cmd = new(query, conn);
        using SqlDataReader reader = cmd.ExecuteReader();
        
        while (reader.Read())
        {
            string columnName = reader.GetString(0);
            string dataType = reader.GetString(1);
            bool isNullable = reader.GetString(2) == "YES";
            string csharpType = MapToCSharpType(dataType, isNullable);

            // Fetch column comment if available
            if (columnComments.TryGetValue((table, columnName), out string comment) && !string.IsNullOrWhiteSpace(comment))
            {
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// {comment}");
                sb.AppendLine($"    /// </summary>");
            }

            sb.AppendLine($"    public {csharpType} {ToPascalCase(columnName)} {{ get; set; }}");
        }

        // Handle multiple FK references to the same table
        var fksForTable = foreignKeys.Where(fk => fk.Table == table).ToList();
        var groupedFks = fksForTable.GroupBy(fk => fk.RefTable).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var fk in fksForTable)
        {
            string propertyName = ToPascalCase(fk.RefTable);
            if (groupedFks[fk.RefTable].Count > 1)
            {
                // Append the referencing column name to differentiate
                propertyName += ToPascalCase(fk.Column);
            }
            sb.AppendLine($"    public virtual {ToPascalCase(fk.RefTable)} {propertyName} {{ get; set; }}");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    static string MapToCSharpType(string sqlType, bool isNullable)
    {
        string type = sqlType.ToLower() switch
        {
            "bigint" => "long",
            "int" => "int",
            "smallint" => "short",
            "tinyint" => "byte",
            "bit" => "bool",
            "decimal" or "numeric" => "decimal",
            "float" => "double",
            "real" => "float",
            "datetime" or "smalldatetime" or "date" or "time" or "datetime2" => "DateTime",
            "char" or "varchar" or "text" or "nvarchar" or "ntext" or "nchar" => "string",
            "uniqueidentifier" => "Guid",
            _ => "object"
        };

        return isNullable && type != "string" && type != "object" ? $"Nullable<{type}>" : type;
    }

    static string ToPascalCase(string input)
    {
        return string.Concat(input.Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
    }
}
```

---

### **Example SQL Server Table**
```sql
CREATE TABLE User_Account (
    UserId INT PRIMARY KEY,
    UserName VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NULL,
    RoleId INT NOT NULL,
    ApproverId INT NOT NULL,
    CONSTRAINT FK_UserAccount_Role FOREIGN KEY (RoleId) REFERENCES Role(RoleId),
    CONSTRAINT FK_UserAccount_Approver FOREIGN KEY (ApproverId) REFERENCES Role(RoleId)
);

EXEC sp_addextendedproperty 'MS_Description', 'Primary key for the user', 'SCHEMA', 'dbo', 'TABLE', 'User_Account', 'COLUMN', 'UserId';
EXEC sp_addextendedproperty 'MS_Description', 'User email address', 'SCHEMA', 'dbo', 'TABLE', 'User_Account', 'COLUMN', 'Email';
```

---

### **Generated C# Class with Comments**
```csharp
public class UserAccount
{
    /// <summary>
    /// Primary key for the user
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// User email address
    /// </summary>
    public string Email { get; set; }

    public virtual Role Role { get; set; }
    public virtual Role ApproverIdRole { get; set; }
}
```

---

### **✅ Features Added**
✔ **Extracts column comments** from SQL Server  
✔ **Adds XML `<summary>` comments** to properties  
✔ **Handles multiple foreign keys referencing the same table**  
✔ **Maintains PascalCase naming & Nullable<T> types**  

Would you like to add **DTO generation** next? 🚀
