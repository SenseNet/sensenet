using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SenseNet.ContentRepository.Storage
{
    /// <summary>
    /// Defines a service that can start when the Repository is starting.
    /// </summary>
    public interface ISnService
    {
        /// <summary>
        /// Starts the service. Called when the Repository is starting.
        /// </summary>
        /// <returns>True if the service has started.</returns>
        bool Start();

        /// <summary>
        /// Shuts down the service. Called when the Repository is finishing.
        /// </summary>
        void Shutdown();

    }
}
