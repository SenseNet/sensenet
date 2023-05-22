using System.Threading;
using SenseNet.ContentRepository.Storage;
using SenseNet.ContentRepository.Storage.Security;
using SenseNet.Storage.Security;

namespace SenseNet.ContentRepository.Security
{
    public class DefaultUserProvider : IUserProvider
    {
        public async System.Threading.Tasks.Task<IUser> LoadAsync(string userNameOrIdOrPath, CancellationToken cancel)
        {
            IUser user = User.Load(userNameOrIdOrPath);
            if (user != null) 
                return user;

            var node = await Node.LoadNodeByIdOrPathAsync(userNameOrIdOrPath, cancel).ConfigureAwait(false);
            if (node != null)
                user = node as IUser;

            return user;
        }
    }
}
