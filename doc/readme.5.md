是的，可以通过 `sys.extended_properties` 获取外键（FK）的注释（Description）。SQL Server 允许为数据库对象（包括表、列、约束等）添加 **Extended Properties** 作为注释。  

---

### **🔹 修改 SQL 查询，获取 FK 的注释**
下面的查询：
1. 仍然获取 **外键约束信息**
2. **新增 `sys.extended_properties`**，获取 FK 的 **Description**（如果存在）

```sql
WITH UniqueConstraints AS (
    SELECT 
        ic.object_id AS TableObjectID,
        ic.column_id AS ColumnID,
        MAX(i.is_unique) AS IsUnique
    FROM sys.index_columns ic
    JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    GROUP BY ic.object_id, ic.column_id
)
SELECT 
    fk.name AS ConstraintName,
    s1.name AS TableSchema,
    tp.name AS TableName, 
    cp.name AS ColumnName, 
    s2.name AS RefSchema,
    tr.name AS RefTable,
    ISNULL(uc.IsUnique, 0) AS IsUnique,
    ep.value AS FK_Description  -- 🔹 这里获取 FK 的注释
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.tables tp ON tp.object_id = fkc.parent_object_id
INNER JOIN sys.schemas s1 ON s1.schema_id = tp.schema_id
INNER JOIN sys.columns cp ON cp.object_id = tp.object_id AND cp.column_id = fkc.parent_column_id
INNER JOIN sys.tables tr ON tr.object_id = fkc.referenced_object_id
INNER JOIN sys.schemas s2 ON s2.schema_id = tr.schema_id
LEFT JOIN UniqueConstraints uc ON cp.object_id = uc.TableObjectID AND cp.column_id = uc.ColumnID
LEFT JOIN sys.extended_properties ep ON ep.major_id = fk.object_id AND ep.minor_id = 0 AND ep.name = 'MS_Description';  -- 🔹 获取 FK 注释
```

---

### **🔹 C# 代码解析 FK 注释**
在 `ForeignKeyInfo` 结构中新增 `Description` 字段：
```csharp
class ForeignKeyInfo
{
    public string ConstraintName { get; set; }
    public string TableSchema { get; set; }
    public string Table { get; set; }
    public string Column { get; set; }
    public string RefSchema { get; set; }
    public string RefTable { get; set; }
    public bool IsUnique { get; set; }
    public string Description { get; set; }  // 🔹 FK 注释
}
```

---

### **🔹 修改 `GetForeignKeys()` 方法**
```csharp
static List<ForeignKeyInfo> GetForeignKeys(SqlConnection conn)
{
    List<ForeignKeyInfo> foreignKeys = new List<ForeignKeyInfo>();

    string query = @"
        WITH UniqueConstraints AS (
            SELECT 
                ic.object_id AS TableObjectID,
                ic.column_id AS ColumnID,
                MAX(i.is_unique) AS IsUnique
            FROM sys.index_columns ic
            JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            GROUP BY ic.object_id, ic.column_id
        )
        SELECT 
            fk.name AS ConstraintName,
            s1.name AS TableSchema,
            tp.name AS TableName, 
            cp.name AS ColumnName, 
            s2.name AS RefSchema,
            tr.name AS RefTable,
            ISNULL(uc.IsUnique, 0) AS IsUnique,
            ep.value AS FK_Description
        FROM sys.foreign_keys fk
        INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
        INNER JOIN sys.tables tp ON tp.object_id = fkc.parent_object_id
        INNER JOIN sys.schemas s1 ON s1.schema_id = tp.schema_id
        INNER JOIN sys.columns cp ON cp.object_id = tp.object_id AND cp.column_id = fkc.parent_column_id
        INNER JOIN sys.tables tr ON tr.object_id = fkc.referenced_object_id
        INNER JOIN sys.schemas s2 ON s2.schema_id = tr.schema_id
        LEFT JOIN UniqueConstraints uc ON cp.object_id = uc.TableObjectID AND cp.column_id = uc.ColumnID
        LEFT JOIN sys.extended_properties ep ON ep.major_id = fk.object_id AND ep.minor_id = 0 AND ep.name = 'MS_Description';";

    using (SqlCommand cmd = new SqlCommand(query, conn))
    using (SqlDataReader reader = cmd.ExecuteReader())
    {
        while (reader.Read())
        {
            foreignKeys.Add(new ForeignKeyInfo
            {
                ConstraintName = reader.GetString(0),
                TableSchema = reader.GetString(1),
                Table = reader.GetString(2),
                Column = reader.GetString(3),
                RefSchema = reader.GetString(4),
                RefTable = reader.GetString(5),
                IsUnique = reader.GetBoolean(6),
                Description = reader.IsDBNull(7) ? null : reader.GetString(7) // 🔹 获取注释
            });
        }
    }
    return foreignKeys;
}
```

---

### **🔹 生成代码时，添加 FK 注释**
```csharp
void GenerateEntityClass(string schema, string table, List<ForeignKeyInfo> foreignKeys)
{
    Console.WriteLine($"public class {schema}{table}");
    Console.WriteLine("{");

    // 获取当前表的主表（如果当前表是子表）
    ForeignKeyInfo parentTable = GetParentTable(schema, table, foreignKeys);
    if (parentTable != null)
    {
        if (!string.IsNullOrEmpty(parentTable.Description))
        {
            Console.WriteLine($"    /// <summary>");
            Console.WriteLine($"    /// {parentTable.Description}");
            Console.WriteLine($"    /// </summary>");
        }
        Console.WriteLine($"    public int {parentTable.Column} {{ get; set; }}");
        Console.WriteLine($"    public virtual {parentTable.RefTable} {parentTable.RefTable} {{ get; set; }}");
    }

    // 获取所有引用当前表的子表
    List<ForeignKeyInfo> childTables = GetChildTables(schema, table, foreignKeys);
    foreach (var child in childTables)
    {
        if (!child.IsUnique)
        {
            if (!string.IsNullOrEmpty(child.Description))
            {
                Console.WriteLine($"    /// <summary>");
                Console.WriteLine($"    /// {child.Description}");
                Console.WriteLine($"    /// </summary>");
            }
            Console.WriteLine($"    public virtual ICollection<{child.Table}> {child.Table}s {{ get; set; }} = new List<{child.Table}>();");
        }
    }

    Console.WriteLine("}");
}
```

---

## **🔹 生成示例**
如果 `credit.OrderItem` 的 FK 约束 `FK_OrderItem_Order` 有注释 **"Order details for each product"**，那么：

### **🎯 生成 `OrderItem` 代码**
```csharp
public class CreditOrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }

    /// <summary>
    /// Order details for each product
    /// </summary>
    public virtual CreditOrder Order { get; set; }
}
```

---

## **✅ 结论**
1. **自动获取 FK 注释**
2. **主表、子表关系正确**
3. **自动添加 `/// <summary>` 注释**
4. **不会错误生成自身 FK**

💡 **这样代码更加清晰、可读性更好，符合 EF 代码风格！** 🚀


好的，让我们实现 `GetParentTable` 和 `GetChildTables` 方法，以便根据提供的表名和模式获取相关的外键关系。

## **🛠 方法实现**

### **1️⃣ `GetParentTable` 方法**
这个方法将检查给定表的外键关系，以找到指向该表的主表。

```csharp
static ForeignKeyInfo GetParentTable(string schema, string table, List<ForeignKeyInfo> foreignKeys)
{
    // 查找外键，检查当前表是否为外键的从表（子表）
    return foreignKeys.FirstOrDefault(fk => fk.TableSchema == schema && fk.Table == table);
}
```

### **2️⃣ `GetChildTables` 方法**
这个方法将查找所有指向给定表的子表。

```csharp
static List<ForeignKeyInfo> GetChildTables(string schema, string table, List<ForeignKeyInfo> foreignKeys)
{
    // 获取所有指向当前表的外键（主表）
    return foreignKeys
        .Where(fk => fk.RefSchema == schema && fk.RefTable == table)
        .ToList();
}
```

## **📝 完整代码示例**

在你的代码生成器中，包含这两个方法的示例：

```csharp
static ForeignKeyInfo GetParentTable(string schema, string table, List<ForeignKeyInfo> foreignKeys)
{
    // 查找外键，检查当前表是否为外键的从表（子表）
    return foreignKeys.FirstOrDefault(fk => fk.TableSchema == schema && fk.Table == table);
}

static List<ForeignKeyInfo> GetChildTables(string schema, string table, List<ForeignKeyInfo> foreignKeys)
{
    // 获取所有指向当前表的外键（主表）
    return foreignKeys
        .Where(fk => fk.RefSchema == schema && fk.RefTable == table)
        .ToList();
}
```

## **🔍 使用示例**

### **在代码生成时**
```csharp
void GenerateEntityClass(string schema, string table, List<ForeignKeyInfo> foreignKeys)
{
    Console.WriteLine($"public class {schema}{table}");
    Console.WriteLine("{");

    // 获取当前表的主表（如果当前表是子表）
    ForeignKeyInfo parentTable = GetParentTable(schema, table, foreignKeys);
    if (parentTable != null)
    {
        // 生成主表的外键属性
        Console.WriteLine($"    public int {parentTable.Column} {{ get; set; }}");
        Console.WriteLine($"    public virtual {parentTable.RefTable} {parentTable.RefTable} {{ get; set; }}");
    }

    // 获取所有引用当前表的子表
    List<ForeignKeyInfo> childTables = GetChildTables(schema, table, foreignKeys);
    foreach (var child in childTables)
    {
        if (!child.IsUnique)
        {
            Console.WriteLine($"    public virtual ICollection<{child.Table}> {child.Table}s {{ get; set; }} = new List<{child.Table}>();");
        }
        else
        {
            // 处理一对一的情况
            Console.WriteLine($"    public virtual {child.Table} {child.Column} {{ get; set; }}");
        }
    }

    Console.WriteLine("}");
}
```

## **✅ 结论**
- **`GetParentTable`**：找到当前表的主表。
- **`GetChildTables`**：找到所有引用当前表的子表。
- **代码生成逻辑可以根据表关系自动生成外键属性**。

这样，你的 **C# 代码** 就能保持与数据库的关系一致性！🚀

如果有任何进一步的修改或需求，请随时告诉我！
