using log4net;
using System;
using System.Reflection;
using Topshelf.HostConfigurators;
using Topshelf.Squirrel.Windows.Builders;
using Topshelf.Squirrel.Windows.Interfaces;

/// <summary>
/// 
/// </summary>
namespace Topshelf.Squirrel.Windows
{
	public class SquirreledHost
	{

        #region Logger

        /// <summary>
        /// Logger Log4Net
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(SquirreledHost));

        #endregion

        /// <summary>
        /// The service name
        /// </summary>
        private readonly string serviceName;

        /// <summary>
        /// The service display name
        /// </summary>
        private readonly string serviceDisplayName;

        /// <summary>
        /// The with overlapping
        /// </summary>
        private readonly bool withOverlapping;

        /// <summary>
        /// The prompt for credentials while installing
        /// </summary>
        private readonly bool promptForCredentialsWhileInstalling;

        /// <summary>
        /// The self updatable service
        /// </summary>
        private readonly ISelfUpdatableService selfUpdatableService;

        /// <summary>
        /// The updater
        /// </summary>
        private readonly IUpdater updater;

        /// <summary>
        /// Initializes a new instance of the <see cref="SquirreledHost"/> class.
        /// </summary>
        /// <param name="selfUpdatableService">The self updatable service.</param>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="serviceDisplayName">Display name of the service.</param>
        /// <param name="updater">The updater.</param>
        /// <param name="withOverlapping">if set to <c>true</c> [with overlapping].</param>
        /// <param name="promptForCredentialsWhileInstalling">if set to <c>true</c> [prompt for credentials while installing].</param>
        public SquirreledHost(
			ISelfUpdatableService selfUpdatableService, 
			string serviceName = null,
			string serviceDisplayName = null, IUpdater updater = null, bool withOverlapping = false, bool promptForCredentialsWhileInstalling = false)
		{
			var assemblyName = Assembly.GetEntryAssembly().GetName().Name;
            this.serviceName = serviceName ?? assemblyName;
			this.serviceDisplayName = serviceDisplayName ?? assemblyName;
			this.selfUpdatableService = selfUpdatableService;
			this.withOverlapping = withOverlapping;
			this.promptForCredentialsWhileInstalling = promptForCredentialsWhileInstalling;
			this.updater = updater;
		}

        /// <summary>
        /// Configures the and run.
        /// </summary>
        /// <param name="configureExt">The configure ext.</param>
        public void ConfigureAndRun(ConfigureExt configureExt = null)
		{
			HostFactory.Run(configurator => { Configure(configurator); configureExt?.Invoke(configurator); });
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config">The configuration.</param>
        public delegate void ConfigureExt(HostConfigurator config);

        /// <summary>
        /// Configures the specified configuration.
        /// </summary>
        /// <param name="config">The configuration.</param>
        private void Configure(HostConfigurator config)
		{
			config.Service<ISelfUpdatableService>(service =>
			{
				service.ConstructUsing(settings => selfUpdatableService);
				service.WhenStarted((s, hostControl) =>
				{
					s.Start();
					return true;
				});
				service.AfterStartingService(() => { updater?.Start(); });
				service.WhenStopped(s => { s.Stop(); });
			});

			config.SetServiceName(serviceName);
			config.SetDisplayName(serviceDisplayName);
			config.StartAutomatically();
			config.EnableShutdown();

			if (promptForCredentialsWhileInstalling)
			{
				config.RunAsFirstPrompt();
			}
			else
			{
				config.RunAsLocalSystem();
			}

			config.AddCommandLineSwitch("squirrel", _ => { });
			config.AddCommandLineDefinition("firstrun", _ => Environment.Exit(0));
			config.AddCommandLineDefinition("obsolete", _ => Environment.Exit(0));
			config.AddCommandLineDefinition("updated", version => { config.UseHostBuilder((env, settings) => new UpdateHostBuilder(env, settings, version, withOverlapping)); });
			config.AddCommandLineDefinition("install", version => { config.UseHostBuilder((env, settings) => new InstallAndStartHostBuilder(env, settings, version)); });
			config.AddCommandLineDefinition("uninstall", _ => { config.UseHostBuilder((env, settings) => new StopAndUninstallHostBuilder(env, settings)); });
		}
	}
}