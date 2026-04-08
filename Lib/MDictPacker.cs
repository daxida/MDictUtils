using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using D = System.Collections.Generic.List<Lib.MDictEntry>;

namespace Lib;

// Reader, actually
public static class MDictPacker
{
    // https://github.com/liuyug/mdict-utils/blob/64e15b99aca786dbf65e5a2274f85547f8029f2e/mdict_utils/writer.py#L509
    public static D PackMddFile(string source)
    {
        D dictionary = [];
        source = Path.GetFullPath(source);

        if (File.Exists(source))
        {
            // Single file
            long size = new FileInfo(source).Length;
            string key = "\\" + Path.GetFileName(source);
            if (Path.DirectorySeparatorChar != '\\')
                key = key.Replace(Path.DirectorySeparatorChar, '\\');

            dictionary.Add(new MDictEntry
            {
                Key = key,
                Pos = 0,
                Path = source,
                Size = size
            });
        }
        else if (Directory.Exists(source))
        {
            // Directory walk
            string relpath = source;
            foreach (var fpath in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                long size = new FileInfo(fpath).Length;
                string key = "\\" + Path.GetRelativePath(relpath, fpath);
                if (Path.DirectorySeparatorChar != '\\')
                    key = key.Replace(Path.DirectorySeparatorChar, '\\');

                dictionary.Add(new MDictEntry
                {
                    Key = key,
                    Pos = 0,
                    Path = fpath,
                    Size = size
                });
            }
        }
        else
        {
            throw new FileNotFoundException($"Path does not exist: {source}");
        }

        return dictionary;
    }

    // https://github.com/liuyug/mdict-utils/blob/master/mdict_utils/writer.py#L425
    public static D PackMdxTxt(string source, Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;
        D dictionary = [];
        List<string> sources = [];
        int nullLength = encoding.GetByteCount("\0");

        if (File.Exists(source))
            sources.Add(source);
        else if (Directory.Exists(source))
            sources.AddRange(Directory.GetFiles(source, "*.txt"));

        foreach (var path in sources)
        {
            byte[] fileBytes = File.ReadAllBytes(path);
            long pos = 0, offset = 0;
            string key = null;
            int lineNum = 0;

            long i = 0;
            while (i < fileBytes.Length)
            {
                // Read a line (detect LF or CRLF)
                long lineStart = i;
                while (i < fileBytes.Length && fileBytes[i] != 10 && fileBytes[i] != 13) i++;
                long lineEnd = i;

                // Detect newline length
                if (i < fileBytes.Length && fileBytes[i] == 13) i++;
                if (i < fileBytes.Length && fileBytes[i] == 10) i++;

                int lineLength = (int)(lineEnd - lineStart);
                string line = encoding.GetString(fileBytes, (int)lineStart, lineLength).Trim();
                lineNum++;

                if (line.Length == 0)
                {
                    if (key == null)
                        throw new Exception($"Error at line {lineNum}: {path}");
                    continue;
                }

                if (line == "</>")
                {
                    if (key == null || offset == pos)
                        throw new Exception($"Error at line {lineNum}: {path}");

                    long size = offset - pos + nullLength;
                    dictionary.Add(new MDictEntry
                    {
                        Key = key,
                        Pos = pos,
                        Path = path,
                        Size = size
                    });
                    key = null;
                }
                else if (key == null)
                {
                    key = line;
                    pos = i; // start of definition
                    offset = pos;
                }
                else
                {
                    offset = i; // keep updating
                }
            }
        }

        return dictionary;
    }
}

