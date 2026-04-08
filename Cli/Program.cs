using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
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
            Description = "Dictionary mdx/mdd file"
        };

        // TODO: This is supposed to have >1 arity
        // TODO: should be a subcommand
        Option<string> addPath = new("--add", "-a")
        {
            Description = "Resource file to add",
        };
        // TODO: should conflict with -a tbh, in python they "get away" because of dispatch order
        // TODO: should be a subcommand
        Option<bool> extractFlag = new("--extract", "-x")
        {
            Description = "Extract mdx/mdd file",
        };
        Option<bool> metaFlag = new("--meta", "-m")
        {
            Description = "Show mdx/mdd meta information",
        };

        rootCommand.Arguments.Add(mdictPath);
        rootCommand.Options.Add(addPath);
        rootCommand.Options.Add(extractFlag);
        rootCommand.Options.Add(metaFlag);

        rootCommand.SetAction(parseResult =>
        {
            var parsedMdictPath = parseResult.GetValue(mdictPath);
            string extension = Path.GetExtension(parsedMdictPath);
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
                    // WARN: We enter here with empty input, f.e. cli -f (since f is not a flag!)
                    Console.WriteLine("Folders are not yet supported");
                    return 1;
                default:
                    Console.WriteLine(
                        $"Unsupported file type: '{extension}'. Only .mdx and .mdd are allowed.");
                    return 1;
            }

            // TODO: if we are mdx, we should only accept txt as in --add

            var parsedAddPath = parseResult.GetValue(addPath);
            var parsedExtractFlag = parseResult.GetValue(extractFlag);
            var parsedMetaFlag = parseResult.GetValue(metaFlag);

            if (parsedAddPath != null && !File.Exists(parsedAddPath) && !Directory.Exists(parsedAddPath))
            {
                Console.WriteLine($"Path does not exist: {parsedAddPath}");
                return 1;
            }

            Run(parsedMdictPath, parsedAddPath, parsedExtractFlag, parsedMetaFlag, isMdd);
            return 0;
        });

        ParseResult parseResult = rootCommand.Parse(args);
        return parseResult.Invoke();
    }

    static void Run(string mdictPath, string addPath, bool extractFlag, bool metaFlag, bool isMdd)
    {
        // Console.WriteLine($"mdictPath @ {mdictPath}");
        // Console.WriteLine($"addPath @ {addPath}");
        // Console.WriteLine($"extractFlag @ {extractFlag}");
        // Console.WriteLine($"isMdd @ {isMdd}");

        if (addPath != null)
        {
            List<MDictEntry> packed = isMdd
                ? MDictPacker.PackMddFile(addPath)
                : MDictPacker.PackMdxTxt(addPath);

            var writer = new MDictWriter(packed, isMdd: isMdd);
            using var outFile = File.Open(mdictPath, FileMode.Create);

            writer.Write(outFile);
        }
        else if (extractFlag)
        {
            // TODO: maybe be able to pass it (the original uses --dir)
            var target = Directory.GetCurrentDirectory();
            target = Path.GetFullPath(target);
            MDictPacker.Unpack(target, mdictPath, isMdd);
        }
        else if (metaFlag)
        {
            MDict m = isMdd ? new MDD(mdictPath) : new MDX(mdictPath);
            Console.WriteLine("Version: \"2.0\""); // le hardcode
            Console.WriteLine($"Record: \"{m.Count}\"");
            foreach ((string key, string value) in m.Header)
            {
                // Not sure why this was done in the original, it seems worse to me...
                var keyTitled = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(key);
                Console.WriteLine($"{keyTitled}: \"{value}\"");
            }
        }
        else
        {
            Console.WriteLine("Unreachable ^TM");
        }
    }
}
