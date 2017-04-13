using log4net;
using log4net.Config;
using SimpleHelper;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;
using Topshelf.Logging;
using Topshelf.Squirrel.Updater;
using Topshelf.Squirrel.Updater.Interfaces;

namespace Topshelf.Squirrel.Updater.Sample
{
    class Program
    {

        #region Definition du logger
        
        private static readonly LogWriter Log = HostLogger.Get(typeof(Program));

        #endregion


        #region Private Variables

        /// <summary>
        /// The URL nuget repositories
        /// </summary>
        private static string _urlNugetRepositories = "https://api.nuget.org/v3/index.json";

        #endregion

        /// <summary>
        /// Mains the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            try
            {
                Log.InfoFormat("##########   Starting service '{0}', V '{1}'   ##########",
                                AssemblyHelper.AssemblyTitle,
                                AssemblyHelper.AssemblyVersion);

                // Add the event handler for handling unhandled  exceptions to the event.
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                // Start Service Updater
                IUpdater selfupdater = null;
                ServiceHosted service = new ServiceHosted();

                IUpdateManager updateManager = null;
                try 
                {
                    Log.Info("Updater Initialisation");
                    updateManager = new UpdateManager(_urlNugetRepositories, AssemblyHelper.AssemblyTitle);
                    selfupdater = new RepeatedTimeUpdater(updateManager).SetCheckUpdatePeriod(TimeSpan.FromMinutes(30));
                    selfupdater.Start();
                } 
                catch(Exception exx)
                {
                    Log.WarnFormat("'{0}' is not installed via Squirrel. Install program first.", AssemblyHelper.AssemblyTitle);
                    Log.Warn(exx);
                }

                // Start TopShelf 
                var x = new SquirreledHost(service, AssemblyHelper.CurrentAssembly, selfupdater, true, RunAS.LocalSystem);

                // If RunAS.RunSpecificUser set login / password
                //x.SetCredentials("", "");

                x.ConfigureAndRun(HostConfig =>
                {
                    HostConfig.Service<ServiceHosted>(s =>
                    {
                        s.ConstructUsing(name => new ServiceHosted());
                        s.WhenStarted(tc => tc.Start());
                        s.WhenStopped(tc => tc.Stop());
                        s.WhenPaused(tc => { });
                        s.WhenContinued(tc => { });
                    });
                    HostConfig.EnableServiceRecovery(rc => rc.RestartService(1));
                    HostConfig.EnableSessionChanged();
                    HostConfig.UseLog4Net();
                    HostConfig.StartAutomatically();
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            finally
            {
                Log.InfoFormat("##########   Stoppping service '{0}', V '{1}'   ##########",
                                AssemblyHelper.AssemblyTitle,
                                AssemblyHelper.AssemblyVersion);
            }
        }

        /// <summary>
        /// Currents the domain_ unhandled exception.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.UnhandledExceptionEventArgs"/> instance containing the event data.</param>
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var report = (Exception)e.ExceptionObject;
            if (report != null)
            {
                Log.Error(report);
            }
        }
    }
}
