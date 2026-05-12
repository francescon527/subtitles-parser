using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test;

class Program
{
    static async Task Main()
    {
        var parser = new SubtitlesParser.Classes.Parsers.SubParser();

        var allFiles = BrowseTestSubtitlesFiles();
        // Old per-file flow kept for traceability:
        // foreach (var file in allFiles)
        // {
        //     using var fileStream = File.OpenRead(file);
        //     var mostLikelyFormat = parser.GetMostLikelyFormat(Path.GetFileName(file));
        //     var items = parser.ParseStream(fileStream, Encoding.UTF8, mostLikelyFormat);
        // }
        var allItems = parser.ParseFiles(allFiles, Encoding.UTF8);
        Console.WriteLine("Total parsed subtitle items from {0} files: {1}", allFiles.Length, allItems.Count);

        var outputTextPath = Path.Combine(Directory.GetCurrentDirectory(), "parsed-subtitles-text.txt");
        using var outputWriter = new StreamWriter(outputTextPath, false, Encoding.UTF8);

        foreach (var item in allItems)
        {
            try
            {
                Console.WriteLine($"{item.StartTime:hh\\:mm\\:ss\\.fff} --> {item.EndTime:hh\\:mm\\:ss\\.fff}");
                if (item.Lines is { Length: > 0 })
                {
                    foreach (var line in item.Lines)
                    {
                        Console.WriteLine(line);
                        outputWriter.WriteLine(line);
                    }
                }

                Console.WriteLine("----------------");
                outputWriter.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failure while printing subtitle item: {0}", ex);
            }
        }

        Console.WriteLine("Saved subtitle text to: {0}", outputTextPath);

        Console.ReadLine();
    }

    private static string[] BrowseTestSubtitlesFiles()
    {
        const string subFilesDirectory = @"Content\TestFiles";
        var currentPath = Directory.GetCurrentDirectory();
        var completePath = Path.Combine(currentPath, subFilesDirectory);

        var allFiles = Directory.GetFiles(completePath);
        return allFiles;
    }
}