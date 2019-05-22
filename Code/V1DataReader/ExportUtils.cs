using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace V1DataReader
{
    internal static class ExportUtils
    {
        internal static string RemoveNPI(string original)
        {
            string result = original;
            string[] patterns = new[] { @"\b((67\d{2})|(4\d{3})|(5[1-5]\d{2})|(6011))-?\s?\d{4}-?\s?\d{4}-?\s?\d{4}\b", 
                                        @"\b([0-6]\d{2}|7[0-6]\d|77[0-2])(\s|\-)?(\d{2})\2(\d{4})\b", 
                                        @"\b[0-9]{8}\b" };

            foreach (string pattern in patterns)
            {
                result = ReplaceAll(result, pattern);
            }
            return result;
        }

        private static bool FindMatch(string evaluate, string pattern)
        {
            return Regex.IsMatch(evaluate, pattern);
        }

        private static string Replace(string original, string pattern, string mask)
        {
            string result;
            string maskedvalue = String.Empty;

            string found = Regex.Match(original, pattern).ToString();

            if (found.Length > 0)
            {

                for (int i = 0; i < found.Length; i++)
                {
                    if (found.Substring(i, 1) != " " && found.Substring(i, 1) != "-")
                    {
                        maskedvalue = maskedvalue + "X";
                    }
                    else
                    {
                        maskedvalue = maskedvalue + found.Substring(i, 1);
                    }
                }

                result = original.Replace(found, maskedvalue);
            }
            else
            {
                result = original;
            }

            return result;
        }

        private static string ReplaceAll(string original, string pattern)
        {
            bool hasNPI = FindMatch(original, pattern);
            string result = original;

            while (hasNPI)
            {
                result = Replace(result, pattern, "X");
                hasNPI = FindMatch(result, pattern);
            }

            return result;
        }

    }
}
