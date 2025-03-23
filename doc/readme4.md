是的，这个代码逻辑可以修改，使其能够**自动生成双向导航属性**（`Order` → `OrderItems`，`OrderItem` → `Order`）。

---

## **🛠 代码修改**
**关键点：**
1. **`Order` 类**
   - 如果 `OrderItem` 里有 `OrderId` 外键，并且不是 `UNIQUE`，则 `Order` 需要 **`ICollection<OrderItem>`**（`1:N` 关系）。
   
2. **`OrderItem` 类**
   - `OrderItem` 需要 **`Order` 引用** 以链回 `Order` 主表（`N:1` 关系）。

---

### **🔹 修改 `GetForeignKeys`**
我们需要在 `ForeignKeyInfo` 里**加入父表信息**，以便代码生成时可以正确识别 **主表 → 子表** 和 **子表 → 主表** 关系。

#### **📝 修改 `ForeignKeyInfo`**
```csharp
public class ForeignKeyInfo
{
    public string ConstraintName { get; set; }
    public string TableSchema { get; set; }
    public string Table { get; set; }  // 子表（从表）
    public string Column { get; set; } // 外键字段（如 OrderItem.OrderId）
    public string RefSchema { get; set; }
    public string RefTable { get; set; } // 主表（主表）
    public bool IsUnique { get; set; } // 是否是唯一约束 (1:1 还是 1:N)
}
```

---

### **🔹 `GetForeignKeys()` 查询**
**✅ 这个查询可以自动检测外键，并判断是否 `UNIQUE`，以便决定 `1:1` 或 `1:N` 关系。**
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
            ISNULL(uc.IsUnique, 0) AS IsUnique
        FROM sys.foreign_keys fk
        INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
        INNER JOIN sys.tables tp ON tp.object_id = fkc.parent_object_id
        INNER JOIN sys.schemas s1 ON s1.schema_id = tp.schema_id
        INNER JOIN sys.columns cp ON cp.object_id = tp.object_id AND cp.column_id = fkc.parent_column_id
        INNER JOIN sys.tables tr ON tr.object_id = fkc.referenced_object_id
        INNER JOIN sys.schemas s2 ON s2.schema_id = tr.schema_id
        LEFT JOIN UniqueConstraints uc ON cp.object_id = uc.TableObjectID AND cp.column_id = uc.ColumnID";

    using (SqlCommand cmd = new SqlCommand(query, conn))
    using (SqlDataReader reader = cmd.ExecuteReader())
    {
        while (reader.Read())
        {
            foreignKeys.Add(new ForeignKeyInfo
            {
                ConstraintName = reader.GetString(0),
                TableSchema = reader.GetString(1),
                Table = reader.GetString(2),  // 子表
                Column = reader.GetString(3), // 外键字段
                RefSchema = reader.GetString(4),
                RefTable = reader.GetString(5), // 主表
                IsUnique = reader.GetBoolean(6)
            });
        }
    }
    return foreignKeys;
}
```

---

## **🔹 代码生成逻辑**
✅ 代码修改，**双向生成**：
- **`Order` 类：`public virtual ICollection<OrderItem> OrderItems { get; set; }`**
- **`OrderItem` 类：`public virtual Order Order { get; set; }`**

```csharp
foreach (var fk in foreignKeys)
{
    string childTable = fk.Table;      // 从表 (OrderItem)
    string parentTable = fk.RefTable;  // 主表 (Order)
    string column = fk.Column;         // 外键字段 (OrderId)

    // **子表 (OrderItem) 生成外键引用**
    Console.WriteLine($"public virtual {parentTable} {column} {{ get; set; }}");

    // **主表 (Order) 生成 ICollection<> (1:N 关系)**
    if (!fk.IsUnique)
    {
        Console.WriteLine($"public virtual ICollection<{childTable}> {childTable}s {{ get; set; }} = new List<{childTable}>();");
    }
}
```

---

## **🔹 生成示例**
### **🟢 `OrderItem.OrderId` 外键**
- **`OrderItem`（子表）** 有 `Order` **(N:1)**
- **`Order`（主表）** 有 `OrderItems` **(1:N)**

```csharp
// OrderItem 关联 Order (N:1)
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }

    // 🔹 子表 (OrderItem) 指向 主表 (Order)
    public virtual Order Order { get; set; }
}

// Order 关联多个 OrderItem (1:N)
public class Order
{
    public int Id { get; set; }
    
    // 🔹 主表 (Order) 包含多个 OrderItem
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
```

---

## **🎯 结论**
✅ **自动生成双向导航属性**
- **`OrderItem`**（子表）有 `Order` 外键，指向 `Order`（主表）
- **`Order`**（主表）有 `ICollection<OrderItem>`，指向 `OrderItem`（子表）

这样你的 **主表（Order）和从表（OrderItem）** 就可以 **双向关联** 了！🚀
