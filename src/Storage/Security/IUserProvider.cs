using System.Threading;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Storage.Security
{
    public interface IUserProvider
    {
        /// <summary>
        /// Loads a user by their name, id or path.
        /// </summary>
        /// <param name="userNameOrIdOrPath">A username, id or path.</param>
        /// <param name="cancel"></param>
        /// <returns>A user instance or null.</returns>
        Task<IUser> LoadAsync(string userNameOrIdOrPath, CancellationToken cancel);
    }
}
