using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using Lib;

namespace Cli;

static class Program
{
    // https://learn.microsoft.com/en-us/dotnet/standard/commandline/
    static int Main(string[] args)
    {
        RootCommand rootCommand = new("MDictUtils CLI");

        Argument<string> mdictPath = new("mdx/mdd file")
        {
            Description = "Dictionary MDX/MDD file"
        };

        // This is supposed to have >1 arity
        Option<string> addPath = new("--add", "-a")
        {
            Description = "Resource file to add",
        };

        rootCommand.Arguments.Add(mdictPath);
        rootCommand.Options.Add(addPath);

        rootCommand.SetAction(parseResult =>
        {
            var parsedMdict = parseResult.GetValue(mdictPath);
            string extension = Path.GetExtension(parsedMdict);
            bool isMdd;
            switch (extension)
            {
                case ".mdx":
                    isMdd = false;
                    break;
                case ".mdd":
                    isMdd = true;
                    break;
                case "":
                    Console.WriteLine("Folders are not yet supported");
                    return 1;
                default:
                    Console.WriteLine(
                        $"Unsupported file type: '{extension}'. Only .mdx and .mdd are allowed.");
                    return 1;
            }

            // TODO: if we are mdx, we should only accept txt as in --add

            var parsedAddOption = parseResult.GetValue(addPath);
            if (parsedAddOption == null)
            {
                Console.WriteLine("The -a/--add flag is mandatory for the moment!");
                return 1;
            }
            if (!File.Exists(parsedAddOption) && !Directory.Exists(parsedAddOption))
            {
                Console.WriteLine($"Path does not exist: {parsedAddOption}");
                return 1;
            }

            Run(parsedMdict, parsedAddOption, isMdd);
            return 0;
        });

        ParseResult parseResult = rootCommand.Parse(args);
        return parseResult.Invoke();
    }

    static void Run(string outputPath, string inputPath, bool isMdd)
    {
        Console.WriteLine($"Reading @ {inputPath}");
        Console.WriteLine($"Writing @ {outputPath}");
        Console.WriteLine($"isMdd @ {isMdd}");

        List<MDictEntry> packed = isMdd
            ? MDictPacker.PackMddFile(inputPath)
            : MDictPacker.PackMdxTxt(inputPath);

        // foreach (var entry in packed)
        // {
        //     Console.WriteLine($"Key: {entry.Key}");
        //     Console.WriteLine($"Path: {entry.Path}");
        //     Console.WriteLine($"Pos: {entry.Pos}");
        //     Console.WriteLine($"Size: {entry.Size}");
        //     Console.WriteLine("----------------------");
        // }

        var writer = new MDictWriter(packed, isMdd: isMdd);
        using var outFile = File.Open(outputPath, FileMode.Create);
        writer.Write(outFile);
    }
}
