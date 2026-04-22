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