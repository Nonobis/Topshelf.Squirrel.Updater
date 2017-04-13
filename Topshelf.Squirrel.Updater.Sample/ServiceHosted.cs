using System.Threading.Tasks;
using Topshelf.Logging;
using Topshelf.Squirrel.Updater.Interfaces;

namespace Topshelf.Squirrel.Updater.Sample
{
    public class ServiceHosted : ISelfUpdatableService
    {
        #region Definition du logger

        /// <summary>
        /// Logger
        /// </summary>
        private static readonly LogWriter Log = HostLogger.Get(typeof(ServiceHosted));

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceHosted"/> class.
        /// </summary>
        public ServiceHosted()
        {
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {

        }

        /// <summary>
        /// Stops the specified host control.
        /// </summary>
        /// <returns></returns>
        public void Stop()
        {
        }

        #endregion
    }
}
