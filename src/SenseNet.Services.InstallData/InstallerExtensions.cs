using SenseNet.ContentRepository;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.InstallData
{
    internal static class InstallerExtensions
    {
        public static async Task LogLineAsync(this RepositoryBuilder builder, string text)
        {
            if (builder.Console != null)
                await builder.Console.WriteLineAsync(text).ConfigureAwait(false);
        }
    }
}
