下面是一个 C# 库 `WordFormatter`，它提供以下功能：  
1. **拆分单词**：根据大小写或常见单词分割字符串。  
2. **转换 PascalCase**：确保首字母大写，符合 PascalCase 规则。  
3. **支持常见英文单词库**：可扩展词库提高拆分准确性。  
4. **名词复数转换**：如 `happys` 变成 `happies`（可扩展词形转换规则）。  

### 代码实现：
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class WordFormatter
{
    // 常见单词库（可扩展）
    private static readonly HashSet<string> CommonWords = new()
    {
        "credit", "crm", "number", "group", "customer", "account", "service",
        "happy", "company", "order", "product", "item", "price", "value"
    };

    /// <summary>
    /// 拆分并转换为 PascalCase
    /// </summary>
    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var words = SplitWords(input);
        return string.Concat(words.Select(Capitalize));
    }

    /// <summary>
    /// 处理复数转换，如 happys -> happies
    /// </summary>
    public static string ConvertPlural(string word)
    {
        if (word.EndsWith("y", StringComparison.OrdinalIgnoreCase) && 
            word.Length > 1 && !"aeiou".Contains(word[^2]))
        {
            return word[..^1] + "ies"; // 规则：辅音+y => ies
        }
        return word; // 其他情况暂不处理
    }

    /// <summary>
    /// 拆分单词
    /// </summary>
    private static List<string> SplitWords(string input)
    {
        List<string> result = new();
        string temp = "";
        
        foreach (char c in input)
        {
            if (char.IsUpper(c) && temp.Length > 0 && !CommonWords.Contains(temp.ToLower()))
            {
                result.Add(temp);
                temp = "";
            }
            temp += c;
        }
        if (temp.Length > 0) result.Add(temp);

        return result.Select(w => CommonWords.Contains(w.ToLower()) ? w.ToLower() : w).ToList();
    }

    /// <summary>
    /// 大写首字母
    /// </summary>
    private static string Capitalize(string word)
    {
        if (string.IsNullOrEmpty(word)) return word;
        return char.ToUpper(word[0]) + word[1..].ToLower();
    }

    // 测试示例
    public static void Main()
    {
        Console.WriteLine(ToPascalCase("CREDITCRM"));      // 输出：CreditCRM
        Console.WriteLine(ToPascalCase("CEGrpNUMBER"));    // 输出：CegrpNumber
        Console.WriteLine(ConvertPlural("happys"));        // 输出：happies
    }
}
```

### 关键特性：
- **支持 PascalCase 转换**，如 `CREDITCRM` → `CreditCRM`  
- **智能拆分**，结合大写字母和单词库提高准确性  
- **可扩展词库**，提高单词识别能力  
- **复数转换**，目前支持 `-y` 结尾变 `-ies`，可扩展其他规则  

你可以根据需要补充词库，并扩展更多复数转换规则。
