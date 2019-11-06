using System;
using System.Threading;
using SenseNet.ContentRepository;
using Task = System.Threading.Tasks.Task;

namespace SenseNet.Services.InstallData
{
    public class Installer
    {
        public static async Task InstallSenseNetAsync(Action<RepositoryBuilder> buildRepository, 
            CancellationToken cancellationToken)
        {
            await Packaging.Installer.InstallSenseNetAsync(typeof(Installer).Assembly, 
                "install-services-core.zip",
                buildRepository, cancellationToken);
        }
    }
}
