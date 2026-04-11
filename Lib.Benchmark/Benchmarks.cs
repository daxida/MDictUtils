using System.Text;
using BenchmarkDotNet.Attributes;

namespace Lib.Benchmark;

public class Benchmarks
{
    private readonly string _textFilePath = Path.GetTempFileName();
    private readonly string _mdxDirectoryPath = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly List<MDictEntry> Entries = [];
    private readonly MDictWriterOptions Options = new(Logging: false);

    [GlobalSetup]
    public void Setup()
    {
        using (FileStream fs = new(_textFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using StreamWriter swriter = new(fs, new UTF8Encoding(false));
            foreach (var number in Enumerable.Range(100_000, 300_000))
            {
                var key = $"{number:X}";
                swriter.WriteLine(key);
                swriter.WriteLine($"This is the definition for the entry with the key {key}.");
                swriter.WriteLine("</>");
            }
        }
        Entries.AddRange(MDictPacker.PackMdxTxt(_textFilePath));

        Directory.CreateDirectory(_mdxDirectoryPath);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_textFilePath))
        {
            File.Delete(_textFilePath);
        }
        if (Directory.Exists(_mdxDirectoryPath))
        {
            Directory.Delete(_mdxDirectoryPath, recursive: true);
        }
    }

    [Benchmark]
    public void BenchmarkTxtParsing()
    {
        MDictPacker.PackMdxTxt(_textFilePath);
    }

    [Benchmark]
    public void BenchmarkMdxWriting()
    {
        var temp = Path.Join(_mdxDirectoryPath, Guid.NewGuid().ToString());
        using FileStream fs = new(temp, FileMode.Create, FileAccess.Write, FileShare.None);
        var mdict = new MDictWriter(Entries, Options);
        mdict.Write(fs);
    }
}
