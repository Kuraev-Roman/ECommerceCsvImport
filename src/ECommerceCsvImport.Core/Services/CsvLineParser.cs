using System.Text;

namespace ECommerceCsvImport.Services;


public static class CsvLineParser
{
    private const char Delimiter = ',';
    private const char Quote = '"';

    public static IReadOnlyList<string> Parse(string line)
    {
        var fields = new List<string>();
        if (line == null)
            return fields;

        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == Quote)
                {
                   
                    if (i + 1 < line.Length && line[i + 1] == Quote)
                    {
                        current.Append(Quote);
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == Quote)
            {
                inQuotes = true;
            }
            else if (c == Delimiter)
            {
                fields.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString().Trim());
        return fields;
    }
}