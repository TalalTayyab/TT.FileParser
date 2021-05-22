using System;
using System.Text.RegularExpressions;

namespace TT.FileParserFunction
{
    public class ParseLine
    {
        public bool IsMatch(string input, string pattern)
        {
            //\ba(.*)d\b
            if (string.IsNullOrEmpty(input))
                return false;

            pattern = @"\b" + pattern.Replace("*", "(.*)").Replace("?", ".") + @"\b";
            var regEx = new Regex(pattern, RegexOptions.IgnoreCase);
            return regEx.IsMatch(input);
        }
    }
}
