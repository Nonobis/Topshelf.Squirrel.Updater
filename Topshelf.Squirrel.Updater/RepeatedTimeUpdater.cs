using System;
using System.Reflection;
using System.Threading.Tasks;
using NuGet;
using Squirrel;
using Topshelf.Squirrel.Updater.Interfaces;
using Topshelf.Logging;

namespace Topshelf.Squirrel.Updater
{
    public class RepeatedTimeUpdater : IUpdater
    {

        #region Logger

        private static readonly LogWriter Log = HostLogger.Get(typeof(RepeatedTimeUpdater));

        #endregion

        /// <summary>
        /// The check update period
        /// </summary>
        private TimeSpan checkUpdatePeriod = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The update manager (Will be removed)
        /// </summary>
        private IUpdateManager updateManager;

        /// <summary>
        /// The curversion
        /// </summary>
        private string _CurrentVersion;

        /// <summary>
        /// Url Repositories
        /// </summary>
        private string _UrlOrPath;

        /// <summary>
        /// ApplicationName
        /// </summary>
        private string _ApplicationName;

        /// <summary>
        /// Set update Period
        /// </summary>
        /// <param name="checkSpan"></param>
        /// <returns></returns>
        public RepeatedTimeUpdater SetCheckUpdatePeriod(TimeSpan checkSpan)
        {
            checkUpdatePeriod = checkSpan;
            return this;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RepeatedTimeUpdater"/> class.
        /// </summary>
        /// <param name="pUrlOrPath">url or path containing updates.</param>
        /// <param name="applicationName">application name.</param>
        public RepeatedTimeUpdater(string pUrlOrPath, string pApplicationName = null)
        {
            _CurrentVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
            _UrlOrPath = pUrlOrPath;
            _ApplicationName = pApplicationName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RepeatedTimeUpdater"/> class.
        /// </summary>
        /// <param name="pUpdateManager">The update manager.</param>
        /// <exception cref="Exception">Update manager can not be null</exception>
        [Obsolete("Will be removed in next release")]
        public RepeatedTimeUpdater(IUpdateManager pUpdateManager)
        {
            _CurrentVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
            updateManager = pUpdateManager;
            if (updateManager == null)
                throw new Exception("Update manager can not be null");
        }

        /// <summary>
        /// Start Update task
        /// </summary>
        public void Start()
        {
            if (!string.IsNullOrEmpty(_UrlOrPath) && !string.IsNullOrEmpty(_ApplicationName))
            {
                Task.Run(Update).ConfigureAwait(false);
            }
            else
            {
                // Will be removed
                Task.Run(OldUpdate).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Updates this instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Update manager can not be null</exception>
        [Obsolete("Will be removed in next release")]
        private async Task OldUpdate()
        {
            if (updateManager == null)
                throw new Exception("Update manager can not be null");

            Log.InfoFormat("Automatic-renewal was launched ({0})", _CurrentVersion);
            while (true)
            {
                await Task.Delay(checkUpdatePeriod);
                try
                {
                    // Check for update
                    var update = await updateManager.CheckForUpdate();
                    try
                    {
                        var oldVersion = update.CurrentlyInstalledVersion?.Version ?? new SemanticVersion(0, 0, 0, 0);
                        Log.InfoFormat("Installed version: {0}", oldVersion);
                        var newVersion = update.FutureReleaseEntry.Version;
                        if (oldVersion < newVersion)
                        {
                            Log.InfoFormat("Found a new version: {0}", newVersion);

                            // Downlaod Release
                            await updateManager.DownloadReleases(update.ReleasesToApply);

                            // Apply Release
                            await updateManager.ApplyReleases(update);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorFormat("Error on update ({0}): {1}", _CurrentVersion, ex);
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Error on check for update ({0}): {1}", _CurrentVersion, ex);
                }
            }
        }

        /// <summary>
        /// Updates this instance.
        /// </summary>
        /// <returns></returns>
        private async Task Update()
        {
            Log.InfoFormat("Automatic-renewal was launched ({0})", _CurrentVersion);
            while (true)
            {
                await Task.Delay(checkUpdatePeriod);

                try
                {
                    using (var upManager = new UpdateManager(_UrlOrPath, _ApplicationName))
                    {
                        // Check for update
                        var update = await upManager.CheckForUpdate();
                        var oldVersion = update.CurrentlyInstalledVersion?.Version ?? new SemanticVersion(0, 0, 0, 0);
                        Log.InfoFormat("Installed version: {0}", oldVersion);

                        var newVersion = update.FutureReleaseEntry?.Version;
                        if (newVersion != null && oldVersion < newVersion)
                        {
                            Log.InfoFormat("Found a new version: {0}", newVersion);

                            // Downlaod Release
                            await upManager.DownloadReleases(update.ReleasesToApply);

                            // Apply Release
                            await upManager.ApplyReleases(update);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }
        }
    }
}