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
        /// The update manager
        /// </summary>
        private IUpdateManager updateManager;

        /// <summary>
        /// The curversion
        /// </summary>
        private string curversion;

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
        /// <param name="pUpdateManager">The update manager.</param>
        /// <exception cref="Exception">Update manager can not be null</exception>
        public RepeatedTimeUpdater(IUpdateManager pUpdateManager)
        {
            curversion = Assembly.GetEntryAssembly().GetName().Version.ToString();
            updateManager = pUpdateManager;
            if (updateManager == null)
                throw new Exception("Update manager can not be null");
        }

        /// <summary>
        /// Start Update task
        /// </summary>
        public void Start()
        {
            Task.Run(Update).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates this instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Update manager can not be null</exception>
        private async Task Update()
        {
            if (updateManager == null)
                throw new Exception("Update manager can not be null");

            Log.InfoFormat("Automatic-renewal was launched ({0})", curversion);

            {
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
                            Log.ErrorFormat("Error on update ({0}): {1}", curversion, ex);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorFormat("Error on check for update ({0}): {1}", curversion, ex);
                    }
                }
            }
        }
    }
}