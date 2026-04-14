using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Lib.BuildModels;

namespace Lib.Build;

internal partial class OffsetTableBuilder
(
    ILogger<OffsetTableBuilder> logger,
    MDictKeyComparer keyComparer
)
{
    public OffsetTable Build(List<MDictEntry> entries, MDictWriterOptions opt)
    {
        entries.Sort((a, b) => keyComparer.Compare(a.Key, b.Key, opt.IsMdd));

        var encodingSettings = GetEncodingSettings(opt);
        var arrayBuilder = ImmutableArray.CreateBuilder<OffsetTableEntry>(entries.Count);
        long currentOffset = 0;

        foreach (var item in entries)
        {
            // Console.WriteLine($"dict item: {item}");
            var keyEnc = encodingSettings.InnerEncoding.GetBytes(item.Key);
            var keyNull = encodingSettings.InnerEncoding.GetBytes($"{item.Key}\0");
            var keyLen = keyEnc.Length / encodingSettings.EncodingLength;

            // var recordNull = encodingSettings.InnerEncoding.GetBytes(item.Path);

            var tableEntry = new OffsetTableEntry
            {
                Key = keyEnc,
                KeyNull = keyNull,
                KeyLen = keyLen,
                // RecordNull = recordNull,
                Offset = currentOffset,
                RecordSize = item.Size,
                RecordPos = item.Pos,
                FilePath = item.Path,
                IsMdd = opt.IsMdd,
            };
            arrayBuilder.Add(tableEntry);

            currentOffset += item.Size;
        }

        // pretty print it here
        // {
        //     Console.WriteLine("---- Offset Table ----");
        //
        //     int index = 0;
        //     foreach (var entry in _offsetTable)
        //     {
        //         string key = _encoding.GetString(entry.Key);
        //         string keyNull = _encoding.GetString(entry.KeyNull);
        //         string recordNull = _encoding.GetString(entry.RecordNull);
        //         string valuePreview = _encoding.GetString(entry.RecordNull)
        //             .TrimEnd('\0')
        //             .Replace("\r", "")
        //             .Replace("\n", " ");
        //
        //         valuePreview = $"{valuePreview[..40]}...";
        //
        //         Console.WriteLine(
        //             $"[{index}] " +
        //             $"Key=\"{key}\", " +
        //             $"Offset={entry.Offset}, " +
        //             $"KeyNull=\"{keyNull}\", " +
        //             $"KeyLen={entry.KeyLen}, " +
        //             $"RecordNull={recordNull}, " +
        //             $"RecordPos={entry.RecordPos}, " +
        //             $"RecordSize={entry.RecordSize}, " +
        //             $"Path=\"{valuePreview}\""
        //         );
        //
        //         index++;
        //     }
        //
        //     Console.WriteLine("----------------------");
        // }

        var tableEntries = arrayBuilder.MoveToImmutable();
        LogInfo(tableEntries.Length, currentOffset);

        return new OffsetTable(tableEntries);
    }

    private sealed record EncodingSettings(
        Encoding InnerEncoding, // _python_encoding in the original
        Encoding Encoding,
        int EncodingLength);

    private static EncodingSettings GetEncodingSettings(MDictWriterOptions opt)
    {
        var encoding = opt.Encoding.ToLower();
        Debug.Assert(encoding == "utf8");

        if (opt.IsMdd || encoding == "utf16" || encoding == "utf-16")
        {
            return new(
                InnerEncoding: Encoding.Unicode,
                Encoding: Encoding.Unicode,
                EncodingLength: 2);
        }
        else if (encoding == "utf8" || encoding == "utf-8")
        {
            return new(
                InnerEncoding: Encoding.UTF8,
                Encoding: Encoding.UTF8,
                EncodingLength: 1);
        }
        else
        {
            throw new ArgumentException("Unknown encoding. Supported: utf8, utf16");
        }
    }

    [LoggerMessage(LogLevel.Debug,
    "Total entries: {Count}, record length {RecordLength}")]
    partial void LogInfo(int count, long RecordLength);
}
