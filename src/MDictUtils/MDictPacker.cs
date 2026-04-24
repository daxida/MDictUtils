using System.Text;

namespace MDictUtils;

/// <summary>
/// Class for both writing (packing) and reading (unpacking)
/// </summary>
public static class MDictPacker
{
    // python does not include the BOM in the title/description
    // so we do the same to allow for oracle testing (but it should not matter really)
    private static readonly UTF8Encoding UTF8NoBOM = new(false);
    private const long MaxRecordSize = 2_000_000_000;

    public static void Unpack(string target, string source, bool isMdd, Encoding? encoding = null)
    {
        // This creates intermediate folders, in case target = d1/d2/folder
        if (!Directory.Exists(target))
        {
            Directory.CreateDirectory(target);
        }

        if (isMdd)
        {
            UnpackMdd(target, source);
        }
        else
        {
            UnpackMdx(target, source, encoding);
        }
    }


    public static void UnpackMdx(string target, string source, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        MDX mdx = new(source, encoding);
        string basename = Path.GetFileName(source);

        Dictionary<string, string> header = mdx.Header;

        if (header.TryGetValue("Description", out var description) && description.Length > 0)
        {
            string descPath = Path.Combine(target, $"{basename}.description.html");
            // Console.WriteLine($"[UnpackMdx] Writing description to {descPath}...");
            using FileStream fs = new(descPath, FileMode.Create, FileAccess.Write);
            using StreamWriter swriter = new(fs, UTF8NoBOM);

            // f.write(b'\r\n'.join(mdx.header[b'Description'].splitlines()))
            // Force CRLF like the python model
            var lines = description.Split(["\r\n", "\n"], StringSplitOptions.None);
            swriter.Write(string.Join("\r\n", lines));
        }

        if (header.TryGetValue("Title", out var title) && title.Length > 0)
        {
            string titlePath = Path.Combine(target, $"{basename}.title.html");
            // Console.WriteLine($"[UnpackMdx] Writing title to {titlePath}...");
            File.WriteAllText(titlePath, title, UTF8NoBOM);
        }

        // We only support split - None
        // Since split is None, we just write everything to a single file
        string outPath = Path.Combine(target, $"{basename}.txt");

        using FileStream outfile = new(outPath, FileMode.Create, FileAccess.Write);

        ReadOnlySpan<byte> whitespaceBytes = encoding.GetBytes(" ");
        ReadOnlySpan<byte> newlineBytes = encoding.GetBytes("\n");
        ReadOnlySpan<byte> carriageNewlineBytes = encoding.GetBytes("\r\n");
        ReadOnlySpan<byte> endOfEntryBytes = encoding.GetBytes("</>");

        int itemCount = 0;

        foreach (var (key, bytes) in mdx.Items())
        {
            // if not value.strip(): continue
            if (bytes.Length == 0 || bytes.Trim(whitespaceBytes).Length == 0)
            {
                continue;
            }

            itemCount++;

            byte[] keyBytes = encoding.GetBytes(key);
            outfile.Write(keyBytes);
            outfile.Write(carriageNewlineBytes);

            outfile.Write(bytes);
            if (bytes.Length == 0 || !bytes.EndsWith(newlineBytes))
            {
                outfile.Write(carriageNewlineBytes);
            }

            outfile.Write(endOfEntryBytes);
            outfile.Write(carriageNewlineBytes);
        }
    }

    public static void UnpackMdd(string target, string source)
    {
        MDD mdd = new(source);
        var datafolder = Path.GetFullPath(target);

        foreach (var (fname, bytes) in mdd.Items())
        {
            // fname = key.decode('UTF-8').replace('\\', os.path.sep)
            // We trim at start, because Path.Combine will not combine if the second arg is a dir...
            var fnameClean = fname.TrimStart('\\').Replace('\\', Path.DirectorySeparatorChar);
            var dfname = Path.Combine(datafolder, fnameClean);
            string? dir = Path.GetDirectoryName(dfname);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            // Console.WriteLine($"[UnpackMdd] {datafolder} | {fnameClean} | {dfname}");
            File.WriteAllBytes(dfname, bytes);
        }

        Console.WriteLine($"Extracted {mdd.Count} entries to {target}");
    }

    // https://github.com/liuyug/mdict-utils/blob/64e15b99aca786dbf65e5a2274f85547f8029f2e/mdict_utils/writer.py#L509
    public static List<MDictEntry> PackMdd(string source)
    {
        List<MDictEntry> entries = [];
        source = Path.GetFullPath(source);

        if (File.Exists(source))
        {
            entries.Add(PackMddFile(source));
        }
        else if (Directory.Exists(source))
        {
            foreach (var fpath in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
                entries.Add(PackMddFile(fpath, source));
        }
        else
        {
            throw new FileNotFoundException($"Path does not exist: {source}");
        }

        return entries;
    }

    public static MDictEntry PackMddFile(string fpath, string? basePath = null)
    {
        // Single file (wtf is happening with separators?)
        var info = new FileInfo(fpath);

        /// TODO: An error will be thrown later if this length is equal to zero.
        /// <see cref="Build.Blocks.MddRecordBlocksBuilder.WriteBytesAsync"/>
        int size = info.Length < MaxRecordSize
            ? Convert.ToInt32(info.Length)
            : throw new InvalidDataException($"File '{info.FullName}' is too large (over {MaxRecordSize:N0} bytes)");

        string relativeName = basePath is not null
            ? Path.GetRelativePath(basePath, fpath)
            : Path.GetFileName(fpath);
        string key = "\\" + relativeName;

        if (Path.DirectorySeparatorChar != '\\')
            key = key.Replace(Path.DirectorySeparatorChar, '\\');

        return new(key, Path: fpath, Pos: 0, size);
    }

    // https://github.com/liuyug/mdict-utils/blob/master/mdict_utils/writer.py#L425
    public static List<MDictEntry> PackMdx(string source, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;

        List<MDictEntry> entries = [];
        List<string> sources = [];

        ReadOnlySpan<byte> lfBytes = encoding.GetBytes("\n");
        ReadOnlySpan<byte> lfcrBytes = encoding.GetBytes("\r\n");
        int nullLength = encoding.GetByteCount("\0");

        if (File.Exists(source))
            sources.Add(source);
        else if (Directory.Exists(source))
            sources.AddRange(Directory.GetFiles(source, "*.txt"));

        foreach (var path in sources)
        {
            byte[] fileBytes = File.ReadAllBytes(path); // TODO: This will crash if the file is too big.
            int pos = 0, offset = 0;
            string? key = null;
            int lineNum = 0;
            int i = 0;

            var bomBytes = encoding.GetPreamble();
            if (fileBytes.StartsWith(bomBytes))
                i += bomBytes.Length;

            while (i < fileBytes.Length)
            {
                int lineStart = i;
                while (i < fileBytes.Length)
                {
                    i++;
                    var currentLine = fileBytes.AsSpan(lineStart..i);
                    if (currentLine.EndsWith(lfBytes))
                        break;
                }

                var fullLine = fileBytes.AsSpan(lineStart..i);
                int lineEnd
                    = fullLine.EndsWith(lfcrBytes)
                        ? i - lfcrBytes.Length
                    : fullLine.EndsWith(lfBytes)
                        ? i - lfBytes.Length
                    : i;

                int lineLength = lineEnd - lineStart;
                string line = encoding.GetString(fileBytes, lineStart, lineLength).Trim();
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

                    int size = offset - pos + nullLength;
                    entries.Add(new MDictEntry(key, path, pos, size));
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

        return entries;
    }
}
