using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace Lib;

public class MDict
{
    readonly Dictionary<string, string> header;

    protected string _fname;
    protected Encoding _encoding;
    protected byte[] _encryptedKey;
    protected float _version;
    protected int _numberWidth;
    protected string _numberFormat;
    protected int _encrypt;
    protected Dictionary<string, (string, string)> _stylesheet = [];
    protected List<(long keyId, string keyText)> _keyList;
    protected long _keyBlockOffset;
    protected long _recordBlockOffset;
    protected int _numEntries;

    public MDict(string fname, Encoding encoding = null, (string regcode, string userid)? passcode = null)
    {
        Debug.Assert(encoding == Encoding.Unicode); // should be ok for MDD
        Debug.Assert(passcode == null);

        _fname = fname;
        _encoding = encoding;
        header = ReadHeader();
        _version = 2.0F; // hardcode it, useless anyway
        _keyList = ReadKeys();
    }

    public int Count => _numEntries;

    // Maybe do the GetEnumerator equivalent of the python code 
    // https://github.com/liuyug/mdict-utils/blob/64e15b99aca786dbf65e5a2274f85547f8029f2e/mdict_utils/base/readmdict.py#L506
    public IEnumerable<(string, byte[])> Items()
    {
        return ReadRecords();
    }

    // _read_records_v1v2
    // https://github.com/liuyug/mdict-utils/blob/64e15b99aca786dbf65e5a2274f85547f8029f2e/mdict_utils/base/readmdict.py#L563
    protected IEnumerable<(string, byte[])> ReadRecords()
    {
        Console.WriteLine($"[ReadRecords] vars: {_fname}");

        using var fs = new FileStream(_fname, FileMode.Open, FileAccess.Read);
        fs.Seek(_recordBlockOffset, SeekOrigin.Begin);
        using var br = new BinaryReader(fs);

        // Read record block header
        long numRecordBlocks = ReadNumber(br);
        long numEntries = ReadNumber(br);
        if (numEntries != _numEntries)
            throw new InvalidDataException($"Number of entries {numEntries} does not match _numEntries {_numEntries}.");
        Console.WriteLine($"[ReadRecords] numEntries: {numEntries}");

        long recordBlockInfoSize = ReadNumber(br);
        long recordBlockSize = ReadNumber(br);

        // Read record block info
        List<(long, long)> recordBlockInfoList = [];
        long sizeCounter = 0;
        for (int i = 0; i < numRecordBlocks; i++)
        {
            long compressedSize = ReadNumber(br);
            long decompressedSize = ReadNumber(br);
            recordBlockInfoList.Add((compressedSize, decompressedSize));
            sizeCounter += _numberWidth * 2; // two numbers per block
        }

        if (sizeCounter != recordBlockInfoSize)
            throw new InvalidDataException("Record block info size mismatch.");

        long offset = 0;
        int keyIndex = 0;
        sizeCounter = 0;

        foreach (var (compressedSize, decompressedSize) in recordBlockInfoList)
        {
            Console.WriteLine($"[ReadRecords] reading record block... with keyIndex = {keyIndex} and _keyList.Count = {_keyList.Count}");
            byte[] compressedBlock = br.ReadBytes((int)compressedSize);
            byte[] recordBlock = DecodeBlock(compressedBlock);
            Console.WriteLine(
                $"[ReadRecords]\ncompressedBlock = {BitConverter.ToString(compressedBlock)}\n" +
                $"recordBlock = {Encoding.UTF8.GetString(recordBlock)}"
            );

            while (keyIndex < _keyList.Count)
            {
                var (recordStart, keyText) = _keyList[keyIndex];
                Console.WriteLine($"[ReadRecords] recordStart {recordStart}, keyText {keyText}");

                // If the current record starts beyond this block, break
                if (recordStart - offset >= recordBlock.Length)
                {
                    break;
                }

                // Determine record end
                long recordEnd = (keyIndex < _keyList.Count - 1)
                    ? _keyList[keyIndex + 1].keyId
                    : recordBlock.Length + offset;

                keyIndex++;
                int start = (int)(recordStart - offset);
                int length = (int)(recordEnd - offset - start);
                byte[] data = new byte[length];
                Array.Copy(recordBlock, start, data, 0, length);

                // Treat record data (e.g., decode or substitute styles)
                // TODO: do that for mdx, we don't care for mdd
                // yield return (keyText, TreatRecordData(data));
                yield return (keyText, data);
            }

            offset += recordBlock.Length;
            sizeCounter += compressedSize;
        }

        if (sizeCounter != recordBlockSize)
            throw new InvalidDataException("Record block size mismatch.");
    }

    public IEnumerable<string> Keys()
    {
        foreach (var (_, keyValue) in _keyList)
            yield return keyValue;
    }

    // def _read_number(self, f):
    //     return unpack(self._number_format, f.read(self._number_width))[0]
    protected long ReadNumber(BinaryReader br)
    {
        byte[] bytes = br.ReadBytes(_numberWidth);
        if (bytes.Length != _numberWidth)
            throw new EndOfStreamException("Unexpected end of file while reading number");

        // reverse for big-endian
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        if (_numberWidth == 4)
            return BitConverter.ToUInt32(bytes, 0);
        else // 8 bytes
            return (long)BitConverter.ToUInt64(bytes, 0);
    }

    static protected int ReadInt32(BinaryReader br) => br.ReadInt32();

    private static readonly Regex HeaderKeyValuesRegex = new(@"(\w+)=""(.*?)""", RegexOptions.Singleline);

    protected Dictionary<string, string> ParseHeader(string headerText)
    {
        Dictionary<string, string> dict = [];
        foreach (Match match in HeaderKeyValuesRegex.Matches(headerText))
        {
            dict[match.Groups[1].Value] = UnescapeEntities(match.Groups[2].Value);
        }
        return dict;
    }

    protected virtual string UnescapeEntities(string value)
    {
        // Simple unescape for common entities; extend if needed
        return value.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");
    }

    protected Dictionary<string, string> ReadHeader()
    {
        using var fs = new FileStream(_fname, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        // number of bytes of header text
        int headerBytesSize = Common.ReadInt32BigEndian(br);
        byte[] headerBytes = br.ReadBytes(headerBytesSize);

        // 4 bytes: Adler32 checksum of header, little endian
        uint adler32 = br.ReadUInt32(); // Little-endian by default in BinaryReader
        if (adler32 != Common.Adler32(headerBytes))
            throw new InvalidDataException("Header Adler32 checksum mismatch.");

        // mark key block offset
        _keyBlockOffset = fs.Position;

        // decode header text
        string headerText;
        if (headerBytes.Length >= 2 && headerBytes[^2] == 0 && headerBytes[^1] == 0)
        {
            headerText = Encoding.Unicode.GetString(headerBytes, 0, headerBytes.Length - 2);
        }
        else
        {
            headerText = Encoding.UTF8.GetString(headerBytes, 0, headerBytes.Length - 1);
        }

        // parse XML-like tags into dictionary
        var headerTag = ParseHeader(headerText);

        // TODO: detect encoding

        // encryption flag
        if (headerTag.TryGetValue("Encrypted", out var encryptedValue))
        {
            if (encryptedValue.Equals("No", StringComparison.OrdinalIgnoreCase))
            {
                _encrypt = 0;
            }
            else if (encryptedValue.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            {
                _encrypt = 1;
            }
            else
            {
                _encrypt = int.Parse(encryptedValue);
            }
        }
        else
        {
            _encrypt = 0;
        }

        // stylesheet parsing
        _stylesheet = [];
        if (headerTag.TryGetValue("StyleSheet", out var styleSheetValue))
        {
            string[] lines = styleSheetValue.Split(["\r\n", "\n"], StringSplitOptions.None);
            for (int i = 0; i + 2 < lines.Length; i += 3)
            {
                _stylesheet[lines[i]] = (lines[i + 1], lines[i + 2]);
            }
        }

        // version and number width
        _version = float.Parse(headerTag["GeneratedByEngineVersion"]);
        if (_version < 2.0)
        {
            _numberWidth = 4;
        }
        else
        {
            _numberWidth = 8;
            if (_version >= 3.0)
                _encoding = Encoding.UTF8;
        }

        return headerTag;
    }

    // _read_keys_v1v2
    // protected virtual List<(long keyId, string keyText)> ReadKeys()
    // {
    //     // Only implement V1/V2 reading for now
    //     using var fs = new FileStream(_fname, FileMode.Open, FileAccess.Read);
    //     fs.Seek(_keyBlockOffset, SeekOrigin.Begin);
    //     using var br = new BinaryReader(fs);
    //
    //     long numKeyBlocks = ReadNumber(br);
    //     _numEntries = (int)ReadNumber(br);
    //     long keyBlockInfoSize = ReadNumber(br);
    //     long keyBlockSize = ReadNumber(br);
    //
    //     byte[] keyBlockInfo = br.ReadBytes((int)keyBlockInfoSize);
    //     var keyBlockInfoList = DecodeKeyBlockInfo(keyBlockInfo);
    //
    //     byte[] keyBlockCompressed = br.ReadBytes((int)keyBlockSize);
    //     return DecodeKeyBlock(keyBlockCompressed, keyBlockInfoList);
    // }

    public List<(long, string)> ReadKeys()
    {
        using FileStream f = new(_fname, FileMode.Open, FileAccess.Read);
        f.Seek(_keyBlockOffset, SeekOrigin.Begin);

        int numBytes = (_version >= 2.0) ? 8 * 5 : 4 * 4;
        byte[] block = new byte[numBytes];
        _ = f.Read(block, 0, numBytes);

        if ((_encrypt & 1) != 0)
        {
            throw new NotImplementedException();
        }

        using MemoryStream sf = new(block);
        using BinaryReader reader = new(sf);

        // Read numbers
        long numKeyBlocks = ReadNumber(reader);
        _numEntries = (int)ReadNumber(reader);
        long keyBlockInfoDecompSize = (_version >= 2.0) ? ReadNumber(reader) : 0;
        long keyBlockInfoSize = ReadNumber(reader);
        long keyBlockSize = ReadNumber(reader);

        if (_version >= 2.0)
        {
            byte[] adlerBytes = new byte[4];
            _ = f.Read(adlerBytes, 0, 4);
            if (BitConverter.IsLittleEndian) Array.Reverse(adlerBytes);
            uint adler32 = BitConverter.ToUInt32(adlerBytes, 0);
            Debug.Assert(adler32 == Common.Adler32(block));
        }

        // Read key block info
        byte[] keyBlockInfo = new byte[keyBlockInfoSize];
        _ = f.Read(keyBlockInfo, 0, keyBlockInfo.Length);
        List<(long, long)> keyBlockInfoList = DecodeKeyBlockInfo(keyBlockInfo);
        Debug.Assert(numKeyBlocks == keyBlockInfoList.Count);

        // Read key block
        byte[] keyBlockCompressed = new byte[keyBlockSize];
        _ = f.Read(keyBlockCompressed, 0, keyBlockCompressed.Length);

        // Extract key block
        List<(long, string)> keyList = DecodeKeyBlock(keyBlockCompressed, keyBlockInfoList);

        _recordBlockOffset = f.Position;

        return keyList;
    }

    // protected List<(long, long)> DecodeKeyBlockInfo(byte[] keyBlockInfo)
    // {
    //     List<(long, long)> list = [];
    //     int i = 0;
    //     while (i < keyBlockInfo.Length)
    //     {
    //         long compressedSize = BitConverter.ToInt64(keyBlockInfo, i);
    //         i += _numberWidth;
    //         long decompressedSize = BitConverter.ToInt64(keyBlockInfo, i);
    //         i += _numberWidth;
    //         list.Add((compressedSize, decompressedSize));
    //     }
    //     return list;
    // }
    //
    // _decode_key_block_info
    protected List<(long, long)> DecodeKeyBlockInfo(byte[] keyBlockInfoCompressed)
    {
        byte[] keyBlockInfo;

        if (_version >= 2)
        {
            // zlib compression check
            if (keyBlockInfoCompressed.Length < 4)
            {
                throw new InvalidDataException(
                    $"Key block info is too short: expected at least 4 bytes, got {keyBlockInfoCompressed.Length} bytes.");
            }

            // check header bytes
            byte[] expectedHeader = [0x02, 0x00, 0x00, 0x00];
            for (int idx = 0; idx < 4; idx++)
            {
                if (keyBlockInfoCompressed[idx] != expectedHeader[idx])
                {
                    throw new InvalidDataException(
                        $"Key block info header mismatch at byte {idx}: expected 0x{expectedHeader[idx]:X2}, got 0x{keyBlockInfoCompressed[idx]:X2}.");
                }
            }

            // decrypt if needed
            if ((_encrypt & 0x02) != 0)
            {
                throw new InvalidDataException("Encryted data, unsupported");
            }

            // decompress zlib
            keyBlockInfo = DecompressZlib([.. keyBlockInfoCompressed.Skip(8)]);

            // adler32 checksum validation
            uint adler32 = ReadUInt32BigEndian(keyBlockInfoCompressed, 4);
            if (adler32 != Common.Adler32(keyBlockInfo))
                throw new InvalidDataException("Key block info Adler32 mismatch.");
        }
        else
        {
            keyBlockInfo = keyBlockInfoCompressed;
        }

        // decode key block info
        List<(long compressedSize, long decompressedSize)> keyBlockInfoList = [];
        int numEntries = 0;
        int i = 0;

        int byteWidth = (_version >= 2) ? 2 : 1;
        int textTerm = (_version >= 2) ? 1 : 0;

        while (i < keyBlockInfo.Length)
        {
            // number of entries in current key block
            numEntries += (int)ReadNumber(keyBlockInfo, i, _numberWidth);
            i += _numberWidth;

            // text head size
            int textHeadSize = (byteWidth == 2)
                ? ReadUInt16BigEndian(keyBlockInfo, i)
                : keyBlockInfo[i];
            i += byteWidth;

            // skip text head
            if (_encoding != Encoding.Unicode)
                i += textHeadSize + textTerm;
            else
                i += (textHeadSize + textTerm) * 2;

            // text tail size
            int textTailSize = (byteWidth == 2)
                ? ReadUInt16BigEndian(keyBlockInfo, i)
                : keyBlockInfo[i];
            i += byteWidth;

            // skip text tail
            if (_encoding != Encoding.Unicode)
                i += textTailSize + textTerm;
            else
                i += (textTailSize + textTerm) * 2;

            // key block compressed size
            long keyBlockCompressedSize = ReadNumber(keyBlockInfo, i, _numberWidth);
            i += _numberWidth;

            // key block decompressed size
            long keyBlockDecompressedSize = ReadNumber(keyBlockInfo, i, _numberWidth);
            i += _numberWidth;

            keyBlockInfoList.Add((keyBlockCompressedSize, keyBlockDecompressedSize));
        }

        // optionally validate number of entries
        // Debug.Assert(numEntries == _numEntries);

        return keyBlockInfoList;
    }

    static private byte[] DecompressZlib(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var z = new ZLibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        z.CopyTo(output);
        return output.ToArray();
    }

    static private long ReadNumber(byte[] buffer, int offset, int numberWidth)
    {
        byte[] slice = [.. buffer.Skip(offset).Take(numberWidth)];
        if (BitConverter.IsLittleEndian)
            Array.Reverse(slice);
        return (numberWidth == 4) ? BitConverter.ToUInt32(slice, 0) : (long)BitConverter.ToUInt64(slice, 0);
    }

    static private int ReadUInt16BigEndian(byte[] buffer, int offset)
    {
        byte[] slice = new byte[2];
        Array.Copy(buffer, offset, slice, 0, 2);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(slice);
        return BitConverter.ToUInt16(slice, 0);
    }

    static private uint ReadUInt32BigEndian(byte[] buffer, int offset)
    {
        byte[] slice = new byte[4];
        Array.Copy(buffer, offset, slice, 0, 4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(slice);
        return BitConverter.ToUInt32(slice, 0);
    }

    // _decode_key_block
    protected List<(long, string)> DecodeKeyBlock(byte[] keyBlockCompressed, List<(long, long)> keyBlockInfoList)
    {
        List<(long, string)> keyList = [];
        int offset = 0;
        foreach (var (compSize, decompSize) in keyBlockInfoList)
        {
            Console.WriteLine($"[DecodeKeyBlock] compSize {compSize} decompSize {decompSize}");
            byte[] block = new byte[compSize];
            // key_block_compressed[offset:offset+compSize]
            Array.Copy(keyBlockCompressed, offset, block, 0, compSize);
            byte[] decompressed = DecodeBlock(block);
            keyList.AddRange(SplitKeyBlock(decompressed));
            offset += (int)compSize;
        }
        return keyList;
    }

    // decompressedSize is only used for compression_method = 1. We only deal with 0, so don't pass it as an argument.
    protected static byte[] DecodeBlock(byte[] block)
    {
        Debug.Assert(block?.Length >= 8, "Block too small");

        uint info = BitConverter.ToUInt32(block, 0); // little-endian
        int compressionMethod = (int)(info & 0xF);
        int encryptionMethod = (int)((info >> 4) & 0xF);
        int encryptionSize = (int)((info >> 8) & 0xFF);
        Console.WriteLine($"[DecodeBlock] {compressionMethod} {encryptionMethod} {encryptionSize}");

        // ---- adler32 (big-endian) ----
        byte[] adlerBytes = new byte[4];
        Array.Copy(block, 4, adlerBytes, 0, 4);
        if (BitConverter.IsLittleEndian) Array.Reverse(adlerBytes);
        uint adler32 = BitConverter.ToUInt32(adlerBytes, 0);

        // ---- encryption key ----
        // byte[] encryptedKey = _encryptedKey;
        // if (encryptedKey == null)
        // {
        // var encryptedKey = Ripemd128.Compute(adlerBytes);
        // }

        // ---- block data ----
        byte[] data = new byte[block.Length - 8];
        Array.Copy(block, 8, data, 0, data.Length);

        Debug.Assert(encryptionSize <= data.Length, "Invalid encryption size");

        // ---- decrypt ---- (assume no encryption)
        var decryptedBlock = data;

        // ---- decompress ----
        byte[] decompressedBlock;
        Debug.Assert(compressionMethod == 2);
        decompressedBlock = DecompressZlib(decryptedBlock);

        // ---- checksum after decompression ----
        Debug.Assert(adler32 == Common.Adler32(decompressedBlock), "Adler32 mismatch after decompression");

        return decompressedBlock;
    }

    // protected List<(long, string)> SplitKeyBlock(byte[] keyBlock)
    // {
    //     var list = new List<(long, string)>();
    //     int index = 0;
    //     while (index < keyBlock.Length)
    //     {
    //         long keyId = BitConverter.ToInt64(keyBlock, index);
    //         index += _numberWidth;
    //         int endIndex = Array.IndexOf(keyBlock, (byte)0, index);
    //         string keyText = Encoding.UTF8.GetString(keyBlock, index, endIndex - index).Trim();
    //         index = endIndex + 1;
    //         list.Add((keyId, keyText));
    //     }
    //     return list;
    // }


    public List<(long, string)> SplitKeyBlock(byte[] keyBlock)
    {
        Debug.Assert(keyBlock != null, "Key block cannot be null");
        Debug.Assert(keyBlock.Length >= _numberWidth, "Key block is too short");

        var keyList = new List<(long, string)>();
        int keyStartIndex = 0;

        while (keyStartIndex < keyBlock.Length)
        {
            Debug.Assert(keyStartIndex + _numberWidth <= keyBlock.Length, "Unexpected end of key block while reading key ID");

            // Read key ID (big-endian like Python's unpack)
            byte[] idBytes = new byte[_numberWidth];
            Array.Copy(keyBlock, keyStartIndex, idBytes, 0, _numberWidth);
            if (BitConverter.IsLittleEndian) Array.Reverse(idBytes);

            long keyId;
            if (_numberWidth == 4)
                keyId = BitConverter.ToUInt32(idBytes, 0);
            else // 8
                keyId = BitConverter.ToInt64(idBytes, 0);

            // Assert key ID is non-negative
            Debug.Assert(keyId >= 0, "Key ID must be non-negative");

            // Determine delimiter
            byte[] delimiter = _encoding == Encoding.Unicode ? [0x00, 0x00] : [0x00];
            int width = delimiter.Length;
            Console.WriteLine($"[SplitKeyBlock] _encoding {_encoding} width {width}");

            // Find the end of the key text
            int i = keyStartIndex + _numberWidth;
            int keyEndIndex = -1;
            while (i <= keyBlock.Length - width)
            {
                if (keyBlock.AsSpan(i, width).SequenceEqual(delimiter))
                {
                    keyEndIndex = i;
                    break;
                }
                i += width;
            }

            if (keyEndIndex == -1)
            {
                keyEndIndex = keyBlock.Length; // fallback like slicing to end
            }

            // key_text = key_block[key_start_index+self._number_width:key_end_index]

            // Assert that we found a delimiter
            // Debug.Assert(keyEndIndex != -1, "Delimiter not found in key block");

            // Extract key text
            int textLength = keyEndIndex - (keyStartIndex + _numberWidth);
            Debug.Assert(textLength >= 0, "Invalid key text length");

            byte[] rawText = new byte[textLength];
            Array.Copy(keyBlock, keyStartIndex + _numberWidth, rawText, 0, textLength);

            // Decode to string (ignore errors like Python)
            string keyText = _encoding.GetString(rawText).Trim('\0').Trim();

            // Assert non-empty key text
            Debug.Assert(!string.IsNullOrEmpty(keyText), "Key text is empty");

            // Add to list
            Console.WriteLine($"[SplitKeyBlock] id/text {keyId} / {keyText}");
            keyList.Add((keyId, keyText));

            // Move to next key
            keyStartIndex = keyEndIndex + width;

            // Assert we don’t go past the block
            // Debug.Assert(keyStartIndex <= keyBlock.Length, "Key start index past end of block");
        }

        // Assert we processed at least one key
        Debug.Assert(keyList.Count > 0, "No keys were found in the key block");

        return keyList;
    }
}

public class MDD : MDict
{
    public MDD(string fname, (string, string)? passcode = null)
        : base(fname, Encoding.Unicode, passcode) { }
}

public class MDX : MDict
{
    private readonly bool _substyle;
    public MDX(string fname, Encoding encoding = null, bool substyle = false, (string, string)? passcode = null)
        : base(fname, encoding, passcode)
    {
        _substyle = substyle;
    }
}
