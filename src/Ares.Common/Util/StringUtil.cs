namespace Ares.Common.Util;

public class StringUtil
{
    static readonly string NumbericChars = "0123456789";
    static readonly string SymbolsChars = "#$%*&_+=^?/";
    static readonly string FullChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
            /* letters */ + "abcdefghijklmnopqrstuvwxyz"
            /* numeric */ + "0123456789"
            /* symbols */ + "#$%*&_+=^?/";
    static readonly string ExclusiveChars = "abcdefghijklmnopqrstuvwxyz"
            /* numeric */ + "0123456789";

    private static readonly Random _random = new Random();

    public static string GenerateExclusiveCode(int length = 8)
    {
        return new string(Enumerable.Range(0, length)
            .Select(_ => ExclusiveChars[_random.Next(ExclusiveChars.Length)])
            .ToArray());
    }

    public static string GenerateCode(int length = 8)
    {
        return new string(Enumerable.Range(0, length)
            .Select(_ => FullChars[_random.Next(FullChars.Length)])
            .ToArray());
    }

    public static string GenerateNumberCode(int length = 8)
    {
        return new string(Enumerable.Range(0, length)
            .Select(_ => NumbericChars[_random.Next(NumbericChars.Length)])
            .ToArray());
    }

    public static string GenerateSymbolsCode(int length = 8)
    {
        return new string(Enumerable.Range(0, length)
            .Select(_ => SymbolsChars[_random.Next(SymbolsChars.Length)])
            .ToArray());
    }
}
