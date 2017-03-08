using System;
using System.IO;
using System.Web;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Portal.Virtualization
{
    internal static class HelperExtensions
    {
        internal static string ChangeBackslashToSlash(this string originalString)
        {
            return originalString.Replace('/', '\\');
        }
    }

    internal class StaticVirtualizedFileHandler : IHttpHandler
    {
        private static readonly double CacheTimeframeInMinutes = 0.1;
        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            var repositoryPath = context.Request.FilePath;

            var fullyQualyfiedPath = string.Concat(
                WebApplication.CacheFolderFileSystemPath,
                repositoryPath.ChangeBackslashToSlash()
                );

            if (IsLockFileExists(fullyQualyfiedPath))
            {
                // File locked - cannot server from filesystem, fallback to the virtual infrastucture
                ProcessRequestUsingVirtualFile(context, repositoryPath);
            }
            else
            {
                // File is not locked, use filesystem cache
                ProcessRequestUsingFilesystemCache(context, repositoryPath, fullyQualyfiedPath);
            }
        }

        private static void ProcessRequestUsingFilesystemCache(HttpContext context, string repositoryPath, string fullyQualyfiedPath)
        {
            // Check the filesystem cache, create the file if not exists
            if (!System.IO.File.Exists(fullyQualyfiedPath))
            {
                byte[] buffer = ReadVirtualFile(repositoryPath);

                string directoryName = Path.GetDirectoryName(fullyQualyfiedPath);
                if (!Directory.Exists(directoryName))
                    System.IO.Directory.CreateDirectory(directoryName);

                using (var cachedFile = System.IO.File.Create(fullyQualyfiedPath))
                {
                    cachedFile.Write(buffer, 0, buffer.Length);
                }
            }
            else
            {
                var servedFromFileSystem = true;

                // check the date
                DateTime lastWriteTime = System.IO.File.GetLastWriteTime(fullyQualyfiedPath);
                DateTime now = DateTime.UtcNow;
                var nodeDescription = NodeHead.Get(repositoryPath);

                if (lastWriteTime.AddMinutes(CacheTimeframeInMinutes) < now)
                {
                    // update if needed
                    CreateLockFile(fullyQualyfiedPath);

                    int maxTryNum = 20;
                    IOException lastError = null;
                    while (--maxTryNum >= 0)
                    {
                        try
                        {
                            System.IO.File.SetLastWriteTime(fullyQualyfiedPath, now);
                            break;
                        }
                        catch (IOException ioex) //TODO: catch block
                        {
                            lastError = ioex;
                            System.Threading.Thread.Sleep(200);
                        }
                    }
                    if (lastError != null)
                        throw new IOException("Cannot write the file: " + fullyQualyfiedPath, lastError);

                    if (nodeDescription.ModificationDate > lastWriteTime)
                    {
                        // refresh file
                        byte[] buffer = ReadVirtualFile(repositoryPath);
                        servedFromFileSystem = false;

                        using (var cachedFile = System.IO.File.Open(fullyQualyfiedPath, FileMode.Truncate, FileAccess.Write))
                        {
                            cachedFile.Write(buffer, 0, buffer.Length);
                        }
                    }

                    DeleteLockFile(fullyQualyfiedPath);
                }

                // only log file download if it was not logged before by the virtual file provider
                if (servedFromFileSystem)
                {
                    // let the client code log file downloads
                    if (nodeDescription != null && ActiveSchema.NodeTypes[nodeDescription.NodeTypeId].IsInstaceOfOrDerivedFrom("File"))
                        ContentRepository.File.Downloaded(nodeDescription.Id);
                }
            }
            
            string extension = System.IO.Path.GetExtension(repositoryPath);
            context.Response.ContentType = MimeTable.GetMimeType(extension);

            context.Response.TransmitFile(fullyQualyfiedPath);
        }

        private static void ProcessRequestUsingVirtualFile(HttpContext context, string repositoryPath)
        {
            byte[] buffer = ReadVirtualFile(repositoryPath);

            context.Response.ClearContent();

            context.Response.OutputStream.Write(buffer, 0, buffer.Length);

            context.Response.ContentType =
                MimeTable.GetMimeType(System.IO.Path.GetExtension(repositoryPath));

            context.Response.End();
        }

        private static bool IsLockFileExists(string fullyQualyfiedPath)
        {
            return System.IO.File.Exists(GetLockFileName(fullyQualyfiedPath));
        }

        private static void CreateLockFile(string fullyQualyfiedPath)
        {
            var lockFile = System.IO.File.Create(GetLockFileName(fullyQualyfiedPath));
            lockFile.Dispose();
        }

        private static void DeleteLockFile(string fullyQualyfiedPath)
        {
            System.IO.File.Delete(GetLockFileName(fullyQualyfiedPath));
        }

        private static string GetLockFileName(string fullyQualyfiedPath)
        {
            return string.Concat(fullyQualyfiedPath, ".lock");
        }

        private static byte[] ReadVirtualFile(string repositoryPath)
        {
            byte[] buffer;
            int length;

            // Read the virtual file
            using (var virtualFile = System.Web.Hosting.VirtualPathProvider.OpenFile(repositoryPath))
            {
                if (virtualFile == null)
                    throw new ApplicationException(string.Format("The virtual file could not be read in the StaticVirtualizedFileHandler for the virtual path '{0}'", repositoryPath));

                length = (int)virtualFile.Length;
                buffer = new byte[length];
                virtualFile.Read(buffer, 0, length);
            }

            return buffer;
        }
    }

}
