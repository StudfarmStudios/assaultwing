using System.IO;
using System.IO.Compression;
using Microsoft.Xna.Framework;

namespace AW2.Helpers
{
    /// <summary>
    /// Read-only access to shipped game content. Desktop uses the normal file
    /// system; KNI's browser TitleContainer performs synchronous HTTP reads.
    /// </summary>
    public static class ContentFileSystem
    {
#if BROWSER
        private static readonly Lazy<string[]> Manifest = new Lazy<string[]>(LoadManifest);
        private static readonly Lazy<Dictionary<string, string>> PackManifest =
            new Lazy<Dictionary<string, string>>(LoadPackManifest);
        private static readonly Dictionary<string, ZipArchive> LoadedPacks =
            new Dictionary<string, ZipArchive>(StringComparer.OrdinalIgnoreCase);
#endif

        public static Stream OpenRead(string path)
        {
#if BROWSER
            var normalized = Normalize(path);
            if (!PackManifest.Value.TryGetValue(normalized, out var packPath))
                return TitleContainer.OpenStream(normalized);

            if (!LoadedPacks.TryGetValue(packPath, out var pack))
            {
                var stream = TitleContainer.OpenStream(packPath);
                if (!stream.CanSeek)
                {
                    var seekableStream = new MemoryStream();
                    stream.CopyTo(seekableStream);
                    stream.Dispose();
                    seekableStream.Position = 0;
                    stream = seekableStream;
                }
                pack = new ZipArchive(stream, ZipArchiveMode.Read);
                LoadedPacks.Add(packPath, pack);
            }

            var entry = pack.GetEntry(normalized);
            if (entry == null)
                throw new FileNotFoundException($"Content pack {packPath} does not contain {normalized}", normalized);

            // Zip entry streams are forward-only. XNA's content readers seek
            // while resolving embedded resources, so expose one seekable asset
            // stream without changing any callers in the original game.
            using var entryStream = entry.Open();
            var assetStream = new MemoryStream((int)entry.Length);
            entryStream.CopyTo(assetStream);
            assetStream.Position = 0;
            return assetStream;
#else
            return File.OpenRead(path);
#endif
        }

        public static bool Exists(string path)
        {
#if BROWSER
            var normalized = Normalize(path);
            return Manifest.Value.Contains(normalized, StringComparer.OrdinalIgnoreCase);
#else
            return File.Exists(path);
#endif
        }

        public static IEnumerable<string> GetFiles(string directory, string searchPattern,
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
#if BROWSER
            var normalizedDirectory = Normalize(directory).TrimEnd('/');
            var prefix = normalizedDirectory.Length == 0 ? string.Empty : normalizedDirectory + "/";
            var suffix = searchPattern.StartsWith("*.", StringComparison.Ordinal)
                ? searchPattern.Substring(1)
                : null;

            return Manifest.Value.Where(path =>
            {
                if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return false;
                var remainder = path.Substring(prefix.Length);
                if (searchOption == SearchOption.TopDirectoryOnly && remainder.Contains('/'))
                    return false;
                if (suffix != null)
                    return path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
                return string.Equals(Path.GetFileName(path), searchPattern, StringComparison.OrdinalIgnoreCase);
            });
#else
            return Directory.GetFiles(directory, searchPattern, searchOption);
#endif
        }

#if BROWSER
        private static string[] LoadManifest()
        {
            using var stream = TitleContainer.OpenStream("content-manifest.txt");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd()
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(Normalize)
                .ToArray();
        }

        private static Dictionary<string, string> LoadPackManifest()
        {
            using var stream = TitleContainer.OpenStream("content-packs.tsv");
            using var reader = new StreamReader(stream);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (reader.ReadLine() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var fields = line.Split('\t');
                if (fields.Length != 2)
                    throw new InvalidDataException("Invalid browser content pack entry: " + line);
                result.Add(Normalize(fields[0]), Normalize(fields[1]));
            }
            return result;
        }

        private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('.', '/');
#endif
    }
}
