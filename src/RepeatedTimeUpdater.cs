using System;
using System.Reflection;
using System.Threading.Tasks;
using NuGet;
using Squirrel;
using Topshelf.Squirrel.Windows.Interfaces;
using log4net;

namespace Topshelf.Squirrel.Windows
{
    public class RepeatedTimeUpdater : IUpdater
    {

        #region Logger

        /// <summary>
        /// Logger Log4Net
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(RepeatedTimeUpdater));

        #endregion

        /// <summary>
        /// The check update period
        /// </summary>
        private TimeSpan checkUpdatePeriod = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The update manager
        /// </summary>
        private readonly IUpdateManager updateManager;

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
        /// <param name="updateManager">The update manager.</param>
        /// <exception cref="Exception">Update manager can not be null</exception>
        public RepeatedTimeUpdater(IUpdateManager updateManager)
        {
            if (updateManager == null)
                throw new Exception("Update manager can not be null");

            curversion = Assembly.GetEntryAssembly().GetName().Version.ToString();
            this.updateManager = updateManager;
        }

        /// <summary>
        /// Метод который проверяет обновления
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