using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
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
        }

        public override void Install(IDictionary stateSaver)
        {
            var topshelfAssembly = Context.Parameters[AssemblyHelper.ExecutablePath];
            stateSaver.Add(AssemblyHelper.AssemblyTitle, topshelfAssembly);
            RunHidden(topshelfAssembly, "/install");
            base.Install(stateSaver);
        }

        public override void Uninstall(IDictionary savedState)
        {
            if (savedState != null)
            {
                var topshelfAssembly = savedState[AssemblyHelper.AssemblyTitle].ToString();
                RunHidden(topshelfAssembly, "/uninstall");
            }
            base.Uninstall(savedState);
        }

        private static void RunHidden(string primaryOutputAssembly, string arguments)
        {
            var startInfo = new ProcessStartInfo(primaryOutputAssembly)
            {
                WindowStyle = ProcessWindowStyle.Normal,
                Arguments = arguments,
                Verb = "runas",
                UseShellExecute = true
            };

            using (var process = Process.Start(startInfo))
            {
                process?.WaitForExit();
            }
        }

    }
}