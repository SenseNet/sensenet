using System.IO.Compression;

namespace SenseNet.Services.InstallData
{
    //UNDONE: [duplicate] move this to the ContentRepo project and delete the SnAdminRuntime version.
    internal interface IUnpacker
    {
        void Unpack(string packagePath, string targetDirectory);
    }

    internal static class Unpacker
    {
        internal static IUnpacker Instance { get; set; } = new ZipUnpacker();

        public static void Unpack(string packagePath, string targetDirectory)
        {
            Instance.Unpack(packagePath, targetDirectory);
        }
    }

    internal class ZipUnpacker : IUnpacker
    {
        public void Unpack(string packagePath, string targetDirectory)
        {
            ZipFile.ExtractToDirectory(packagePath, targetDirectory);
        }
    }
}
