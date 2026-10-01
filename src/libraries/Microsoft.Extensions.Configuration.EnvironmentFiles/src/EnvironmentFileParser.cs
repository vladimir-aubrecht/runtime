// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.Configuration.EnvironmentFiles;

internal static class EnvironmentFileParser
{
    public static IEnumerable<KeyValuePair<string, string>> Parse(Stream stream, EnvironmentFileSyntax syntax)
    {
        using var reader = new StreamReader(stream);
        int lineNumber = 0;
        while (reader.ReadLine() is string rawLine)
        {
            lineNumber++;
            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            if ((syntax & EnvironmentFileSyntax.ExportPrefix) != 0 &&
                line.StartsWith("export", StringComparison.Ordinal) &&
                line.Length > 6 && char.IsWhiteSpace(line[6]))
            {
                line = line.Substring(7).TrimStart();
            }

            int separator = line.IndexOf('=');
            if (separator < 0)
            {
                throw new FormatException(SR.Format(SR.Error_InvalidAssignment, lineNumber));
            }

            string key = line.Substring(0, separator).Trim();
            if (!IsValidName(key))
            {
                throw new FormatException(SR.Format(SR.Error_InvalidName, lineNumber));
            }

            string rawValue = line.Substring(separator + 1);
            string value = rawValue.Trim();
            bool quoted = value.Length > 0 &&
                ((value[0] == '\'' && (syntax & EnvironmentFileSyntax.SingleQuotedValues) != 0) ||
                 (value[0] == '"' && (syntax & EnvironmentFileSyntax.DoubleQuotedValues) != 0));

            if (quoted)
            {
                int endQuote = value.IndexOf(value[0], 1);
                if (endQuote < 0)
                {
                    throw new FormatException(SR.Format(SR.Error_UnterminatedValue, lineNumber));
                }

                string remainder = value.Substring(endQuote + 1).Trim();
                if (remainder.Length > 0 &&
                    !((syntax & EnvironmentFileSyntax.InlineComments) != 0 && remainder[0] == '#'))
                {
                    throw new FormatException(SR.Format(SR.Error_TrailingCharacters, lineNumber));
                }

                value = value.Substring(1, endQuote - 1);
            }
            else if ((syntax & EnvironmentFileSyntax.InlineComments) != 0)
            {
                for (int i = 1; i < rawValue.Length; i++)
                {
                    if (rawValue[i] == '#' && char.IsWhiteSpace(rawValue[i - 1]))
                    {
                        value = rawValue.Substring(0, i).Trim();
                        break;
                    }
                }
            }

            yield return new KeyValuePair<string, string>(key, value);
        }
    }

    private static bool IsValidName(string name)
    {
        if (name.Length == 0 || !IsNameStart(name[0]))
        {
            return false;
        }

        for (int i = 1; i < name.Length; i++)
        {
            if (!IsNameStart(name[i]) && name[i] is not (>= '0' and <= '9'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsNameStart(char value) => value is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';
}
