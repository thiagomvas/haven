namespace Haven.Infrastructure.Utils;

public static class DirectoryUtils
{
    /// <summary>
    /// Recursively copies every file from <paramref name="sourceDir"/> into
    /// <paramref name="destDir"/>, creating directories as needed and overwriting existing files.
    /// </summary>
    public static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            var destPath = Path.Combine(destDir, relative);

            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            File.Copy(file, destPath, overwrite: true);
        }
    }
}