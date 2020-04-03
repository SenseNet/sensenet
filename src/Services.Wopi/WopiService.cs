using System.Collections.Generic;
using System.Linq;
using SenseNet.Configuration;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Services.Wopi
{
    /// <summary>
    /// Registers a WopiNodeOperationValidator instance as an INodeOperationValidator.
    /// </summary>
    internal class WopiService : ISnService
    {
        public static readonly string ExpectedSharedLock = "WOPI_ExpectedSharedLock";

        public static WopiService Instance => Providers.Instance.GetProvider<WopiService>();

        public bool Start()
        {
            if (Instance == null)
                Providers.Instance.SetProvider(typeof(WopiService), this);

            var checkers = Providers.Instance.GetProvider<List<INodeOperationValidator>>("NodeOperationValidators");
            if (checkers == null)
            {
                checkers = new List<INodeOperationValidator>();
                Providers.Instance.SetProvider("NodeOperationValidators", checkers);
            }

            if (checkers.All(x => x.GetType() != typeof(WopiNodeOperationValidator)))
                checkers.Add(new WopiNodeOperationValidator());

            return true;
        }
        public void Shutdown()
        {
            var checkers = Providers.Instance.GetProvider<List<INodeOperationValidator>>("NodeOperationValidators");
            var checker = checkers?.FirstOrDefault(x => x.GetType() != typeof(WopiNodeOperationValidator));
            if (checker != null)
                checkers.Remove(checker);
        }
    }
}
