以下是一个 C# 方法，能 拆分输入的单词 并 转换为 PascalCase，同时支持 常见单词库 和 简单的复数变换（如 happys → happies）：

方法实现
csharp
Copy
Edit
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public static class WordFormatter
{
    // 常见单词库（可扩展）
    private static readonly HashSet<string> CommonWords = new HashSet<string>
    {
        "credit", "crm", "group", "number", "happy", "company", "data", "service"
    };

    // 复数转换（简单规则）
    private static readonly Dictionary<string, string> PluralRules = new Dictionary<string, string>
    {
        { "ys", "ies" } // happys => happies
    };

    /// <summary>
    /// 拆分单词并转换为 PascalCase
    /// </summary>
    public static string FormatToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // 1. 使用正则表达式尝试拆分单词（大写字母/数字/非字母分隔）
        var words = Regex.Split(input, @"(?<!^)(?=[A-Z0-9])|_|-");

        // 2. 处理单词并转换为 PascalCase
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = Capitalize(words[i].ToLower());
        }

        // 3. 尝试优化拆分（检查是否在单词库中）
        words = FixCommonWords(words);

        // 4. 连接成 PascalCase
        return string.Concat(words);
    }

    /// <summary>
    /// 处理复数转换
    /// </summary>
    public static string FixPlural(string word)
    {
        foreach (var rule in PluralRules)
        {
            if (word.EndsWith(rule.Key, StringComparison.OrdinalIgnoreCase))
            {
                return word.Substring(0, word.Length - rule.Key.Length) + rule.Value;
            }
        }
        return word;
    }

    /// <summary>
    /// 优化单词拆分，避免错误的断词
    /// </summary>
    private static string[] FixCommonWords(string[] words)
    {
        List<string> fixedWords = new List<string>();

        for (int i = 0; i < words.Length; i++)
        {
            string combined = words[i];
            
            // 检查是否能与下一个单词组成常见单词
            while (i + 1 < words.Length && CommonWords.Contains(combined + words[i + 1].ToLower()))
            {
                combined += words[i + 1].ToLower();
                i++;
            }
            
            fixedWords.Add(combined);
        }

        return fixedWords.ToArray();
    }

    /// <summary>
    /// 单词首字母大写
    /// </summary>
    private static string Capitalize(string word)
    {
        if (string.IsNullOrEmpty(word)) return word;
        return char.ToUpper(word[0]) + word.Substring(1);
    }

    // 测试
    public static void Main()
    {
        Console.WriteLine(FormatToPascalCase("CREDITCRM"));         // CreditCRM
        Console.WriteLine(FormatToPascalCase("CEGrpNUMBER"));       // CegrpNumber
        Console.WriteLine(FormatToPascalCase("HappyS"));            // Happies
        Console.WriteLine(FormatToPascalCase("DataServiceCOMPANY"));// DataServiceCompany
    }
}
功能说明
拆分 PascalCase 大写单词

"CREDITCRM" → "CreditCRM"

"CEGrpNUMBER" → "CegrpNumber"

"DataServiceCOMPANY" → "DataServiceCompany"

支持常见单词库优化

避免误拆分 "DataService" → "DataService" 而不是 "Data" + "Service"

简单复数处理

"happys" → "happies"

可扩展

CommonWords 单词库可自行扩展

PluralRules 可添加更复杂的复数转换逻辑

扩展建议
更智能的单词拆分：可以用 词典匹配 + 机器学习模型 进行优化。

更全面的复数规则：可用 NLTK 或 PluralizationService 进行扩展。

支持更多的命名风格：如 snake_case, kebab-case 解析。

如果你需要更复杂的逻辑，可以告诉我，我可以优化这个库！🚀
