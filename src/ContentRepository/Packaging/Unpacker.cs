using System.IO;
using System.IO.Compression;

namespace SenseNet.Packaging
{
    internal interface IUnpacker
    {
        void Unpack(string packagePath, string targetDirectory);
        void Unpack(Stream packageStream, string targetDirectory);
    }

    internal static class Unpacker
    {
        internal static IUnpacker Instance { get; set; } = new ZipUnpacker();

        public static void Unpack(string packagePath, string targetDirectory)
        {
            Instance.Unpack(packagePath, targetDirectory);
        }
        public static void Unpack(Stream packageStream, string targetDirectory)
        {
            Instance.Unpack(packageStream, targetDirectory);
        }
    }

    internal class ZipUnpacker : IUnpacker
    {
        public void Unpack(string packagePath, string targetDirectory)
        {
            ZipFile.ExtractToDirectory(packagePath, targetDirectory);
        }
        public void Unpack(Stream packageStream, string targetDirectory)
        {
            using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read);
            archive.ExtractToDirectory(targetDirectory);
        }
    }
}
