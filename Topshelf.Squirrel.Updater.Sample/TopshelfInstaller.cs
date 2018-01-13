using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using SimpleHelper;

namespace Topshelf.Squirrel.Updater.Sample
{
    /// <summary>
    /// Class TopshelfInstaller.
    /// </summary>
    [RunInstaller(true)]
    public partial class TopshelfInstaller : Installer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TopshelfInstaller"/> class.
        /// </summary>
        public TopshelfInstaller()
        {
            using (var serviceInstaller = new ServiceInstaller())
            {
                using (var serviceProcessInstaller = new ServiceProcessInstaller {Account = ServiceAccount.LocalService})
                {
                    serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
                    serviceProcessInstaller.Username = null;
                    serviceProcessInstaller.Password = null;

                    serviceInstaller.DisplayName = AssemblyHelper.AssemblyTitle;
                    serviceInstaller.ServiceName = AssemblyHelper.AssemblyTitle;
                    serviceInstaller.Description = AssemblyHelper.AssemblyDescription;
                    serviceInstaller.DelayedAutoStart = false;
                    serviceInstaller.StartType = ServiceStartMode.Automatic;
                    Installers.AddRange(new Installer[] { serviceProcessInstaller, serviceInstaller });
                }
            }
            Committed += ServiceInstaller_Committed;
        }

        /// <summary>
        /// Handles the Committed event of the ServiceInstaller control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="InstallEventArgs"/> instance containing the event data.</param>
        void ServiceInstaller_Committed(object sender, InstallEventArgs e)
        {
            // Auto Start the Service Once Installation is Finished.
            using (var controller = new ServiceController(AssemblyHelper.AssemblyTitle))
            {
                controller.Start();
            }
        }
    }
}