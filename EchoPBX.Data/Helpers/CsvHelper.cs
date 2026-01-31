using System.Text;

namespace EchoPBX.Data.Helpers;

/// <summary>
/// Helper class for parsing CSV lines
/// </summary>
public static class CsvHelper
{
    /// <summary>
    /// Parses a single CSV line into a list of fields
    /// </summary>
    /// <param name="line">The CSV line to parse</param>
    public static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var currentField = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check if this is an escaped quote
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++; // skip next quote
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        result.Add(currentField.ToString());
                        currentField.Clear();
                        break;
                    default:
                        currentField.Append(c);
                        break;
                }
            }
        }

        // Add last field
        result.Add(currentField.ToString());

        return result;
    }
}