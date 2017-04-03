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
using Topshelf.Squirrel.Updater.Interfaces;

namespace Topshelf.Squirrel.Updater.Sample
{
    class Program
    {
        #region Logger

        /// <summary>
        /// The log
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        #endregion

        #region Private Variables

        /// <summary>
        /// The URL nuget repositories
        /// </summary>
        private static string _urlNugetRepositories = "http://nuget.itoo.me/feeds/PublicApplication";

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
                try
                {
                    Log.Info("Updater Initialisation");
                    IUpdateManager updateManager = new UpdateManager(_urlNugetRepositories);
                    selfupdater = new RepeatedTimeUpdater(updateManager)
                        .SetCheckUpdatePeriod(TimeSpan.FromMinutes(30));
                    selfupdater.Start();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    Log.Error("Sorry can't start updater ...");
                }
                
                // Start TopShelf 
                var x = new SquirreledHost(service, AssemblyHelper.AssemblyTitle, AssemblyHelper.AssemblyTitle, selfupdater, true, false);
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
                    HostConfig.RunAsLocalSystem();
                    HostConfig.SetDescription(AssemblyHelper.AssemblyDescription);
                    HostConfig.SetDisplayName(AssemblyHelper.AssemblyTitle);
                    HostConfig.SetServiceName(AssemblyHelper.AssemblyTitle);
                    HostConfig.RunAsLocalSystem();
                    HostConfig.UseAssemblyInfoForServiceInfo();
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
