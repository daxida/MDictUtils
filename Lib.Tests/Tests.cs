using System;
using System.IO;
using System.Text;
using Xunit;

using D = System.Collections.Generic.List<Lib.MDictEntry>;

namespace Lib.Tests;

public class MDictWriterTests
{
    [Fact]
    public void Constructor_WithEmptyDictionary_Succeeds()
    {
        var dictionary = new D();
        var writer = new MDictWriter(dictionary);
        Assert.NotNull(writer);
    }

    [Fact]
    public void Write_CreatesValidFile()
    {
        var dictionary = new D();
        var writer = new MDictWriter(dictionary,
            title: "Test Dictionary",
            description: "A test dictionary");
        var outputPath = Path.GetTempFileName();

        try
        {
            using (var outFile = File.Open(outputPath, FileMode.Create))
            {
                writer.Write(outFile);
            }

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
    public void Write_WithUTF8Encoding_CreatesFile()
    {
        var dictionary = new D();
        var writer = new MDictWriter(dictionary, encoding: "utf8");
        var outputPath = Path.GetTempFileName();

        try
        {
            using (var outFile = File.Open(outputPath, FileMode.Create))
            {
                writer.Write(outFile);
            }

            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}

public class Adler32Tests
{
    [Fact]
    public void Adler_StaticMethod_Test()
    {
        string[] parts =
        [
            "asdf",
                "12341234asdfasdf",
            ];

        uint[] expected =
        [
            0x040f019f, // big endian
                0x224e04d1,
            ];

        int i = 0;
        foreach (var part in parts)
        {
            byte[] data = Encoding.UTF8.GetBytes(part);
            uint adlerValue = MdxBlock.Adler32(data);
            Assert.Equal(expected[i], adlerValue);
            i++;
        }

        byte[] data2 =
        [
            0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x02,
                0x00,0x05, (byte)'a', (byte)'p', (byte)'p', (byte)'l', (byte)'e',
                0x00,0x00,
                0x06, (byte)'b', (byte)'a', (byte)'n', (byte)'a', (byte)'n', (byte)'a',
                0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
                0x21,  // <- THIS is the correct byte, 0x21 = '!' 
                0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x1d
        ];
        uint adlerValue2 = MdxBlock.Adler32(data2); // Replace with your static method
        Assert.Equal((uint)1872954559, adlerValue2);
        // python -c "import zlib, struct; data = b'\x00\x00\x00\x00\x00\x00\x00\x02\x00\x05apple\x00\x00\x06banana\x00\x00\x00\x00\x00\x00\x00\x00!\x00\x00\x00\x00\x00\x00\x00\x1d'; checksum = zlib.adler32(data) & 0xffffffff; print('Little-endian:', struct.pack('<L', checksum)); print('Big-endian:', struct.pack('>L', checksum)); print(checksum)"
        //
        // Little-endian: b'\xbf\x04\xa3o'
        // Big-endian: b'o\xa3\x04\xbf'
        // 1872954559
    }
}

// Pack and Unpack should be reversable
public class DoUndoTests
{
    [Fact]
    public void DoUndo_PackAndUnpackMdd_ProducesIdenticalFile()
    {
        const string testContent = @"apple
A fruit that grows on trees.
</>
banana
A long yellow fruit.
</>";

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        string originalStubPath = Path.Combine(tempDir, "stub.txt");
        string outMddPath = Path.Combine(tempDir, "out1.mdd");
        string extractedStubPath = Path.Combine(tempDir, "stub.txt");

        try
        {
            // Create stub.txt
            File.WriteAllText(originalStubPath, testContent);

            // Pack stub.txt into out1.mdd
            var packedEntries = MDictPacker.PackMddFile(originalStubPath);
            var writer = new MDictWriter(packedEntries, isMdd: true);
            using (var outFile = File.Open(outMddPath, FileMode.Create))
            {
                writer.Write(outFile);
            }

            File.Delete(originalStubPath);

            // Unpack out1.mdd to tempDir and compare normalized
            MDictPacker.Unpack(tempDir, outMddPath, isMdd: true);
            Assert.True(File.Exists(extractedStubPath), "Extracted file should exist");
            string extractedContent = File.ReadAllText(extractedStubPath);
            string normalizedOriginal = testContent.Replace("\r\n", "\n").Replace("\r", "\n");
            string normalizedExtracted = extractedContent.Replace("\r\n", "\n").Replace("\r", "\n");
            Assert.Equal(normalizedOriginal, normalizedExtracted);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
