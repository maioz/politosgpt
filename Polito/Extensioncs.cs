using System.Text.RegularExpressions;

namespace PolitoGPT;

public static class Extensions
{
    private const RegexOptions RegexGlobalOptions =
        RegexOptions.Multiline |
        RegexOptions.Singleline |
        RegexOptions.IgnoreCase;


    public static MatchCollection Matchs(this string str, string pattern)
    {
        return Regex.Matches(str, pattern, RegexGlobalOptions, TimeSpan.FromSeconds(5));
    }

    public static Match Match(this string str, string pattern)
    {
        return str.Matchs(pattern).First();
    }
}