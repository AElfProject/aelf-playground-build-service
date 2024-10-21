using System.IO.Compression;

namespace GrainUtilities;

public static class ZipUtilities
{
    public static byte[] ZipFolderToByteArray(string sourceFolder)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var files = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
                string baseFolder = Path.GetFullPath(sourceFolder);

                foreach (var file in files)
                {
                    string entryName = Path.GetRelativePath(baseFolder, file);
                    var entry = archive.CreateEntryFromFile(file, entryName);
                }
            }

            return memoryStream.ToArray();
        }
    }
}