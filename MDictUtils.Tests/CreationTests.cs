using System;
using System.IO;
using System.Threading.Tasks;
using MDictUtils.Creation;
using Xunit;

namespace MDictUtils.Tests;

public class MDictCreationTests
{
    [Fact]
    public async Task Write_CreatesValidFile()
    {
        var creator = new MdxCreator();
        var header = new MdxHeader
        {
            Title = "Test Dictionary",
            Description = "A test dictionary",
        };
        var outputPath = Path.GetTempFileName();

        try
        {
            await creator.WriteAsync(header, outputPath);
            Assert.True(File.Exists(outputPath));
            var fileInfo = new FileInfo(outputPath);
            Assert.True(fileInfo.Length > 0, "File should not be empty");
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task Write_WithUTF8Encoding_CreatesFile()
    {
        var creator = new MdxCreator();
        var header = new MdxHeader();
        var outputPath = Path.GetTempFileName();
        try
        {
            await creator.WriteAsync(header, outputPath, static options
                => options.KeyEncoding = MDictKeyEncodingType.Utf8);

            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}

public class CreationDoUndoTests
{
    private static void AssertContentEqual(string expected, string actual)
    {
        string normalizedExpected = expected.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd('\n');
        string normalizedActual = actual.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd('\n');
        Assert.Equal(normalizedExpected, normalizedActual);
    }

    private const string TestContent =
        """
        apple
        A fruit that grows on trees.
        </>
        banana
        A long yellow fruit.
        </>
        @cc-100
        xxx
        </>
        """;

    [Fact]
    public async Task DoUndo_PackAndUnpackMdx_ProducesIdenticalFile()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        string originalDictPath = Path.Combine(tempDir, "dict1.txt");
        string outMdxPath = Path.Combine(tempDir, "out.mdx");
        string extractedDictPath = Path.Combine(tempDir, "out.mdx.txt");

        try
        {
            // Create dict1.txt
            File.WriteAllText(originalDictPath, TestContent);

            // Pack it into out.mdx
            var creator = new MdxCreator();
            creator.AddEntry("apple", "A fruit that grows on trees.");
            creator.AddEntry("banana", "A long yellow fruit.");
            creator.AddEntry("@cc-100", "xxx");

            var header = new MdxHeader();
            await creator.WriteAsync(header, outMdxPath);

            // Unpack out1.mdx to tempDir and compare normalized
            MDictPacker.Unpack(tempDir, outMdxPath, isMdd: false);
            Assert.True(File.Exists(extractedDictPath), "Extracted file should exist");
            string extractedContent = File.ReadAllText(extractedDictPath);
            AssertContentEqual(TestContent, extractedContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
