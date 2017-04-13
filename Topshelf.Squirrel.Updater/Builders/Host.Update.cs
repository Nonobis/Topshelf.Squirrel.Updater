using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using Topshelf.Builders;
using Topshelf.Logging;
using Topshelf.Runtime;
using Topshelf.Runtime.Windows;

/// <summary>
/// 
/// </summary>
namespace Topshelf.Squirrel.Updater.Builders
{
	public sealed class UpdateHostBuilder : HostBuilder
	{

        #region Logger

        private static readonly LogWriter Log = HostLogger.Get(typeof(UpdateHostBuilder));

        #endregion

        /// <summary>
        /// The stop old host builder
        /// </summary>
        private readonly StopBuilder _stopOldHostBuilder;

        /// <summary>
        /// The start old host builder
        /// </summary>
        private readonly StartBuilder _startOldHostBuilder;

        /// <summary>
        /// The uninstall old host builder
        /// </summary>
        private readonly UninstallBuilder _uninstallOldHostBuilder;

        /// <summary>
        /// The install and start new host builder
        /// </summary>
        private readonly InstallAndStartHostBuilder _installAndStartNewHostBuilder;

        /// <summary>
        /// The stop and uninstall new host builder
        /// </summary>
        private readonly StopAndUninstallHostBuilder _stopAndUninstallNewHostBuilder;

        /// <summary>
        /// Gets the environment.
        /// </summary>
        /// <value>
        /// The environment.
        /// </value>
        public HostEnvironment Environment { get; }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        public HostSettings Settings { get; }

        /// <summary>
        /// Gets a value indicating whether [with overlapping].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [with overlapping]; otherwise, <c>false</c>.
        /// </value>
        public bool WithOverlapping { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateHostBuilder"/> class.
        /// </summary>
        /// <param name="environment">The environment.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="version">The version.</param>
        /// <param name="withOverlapping">if set to <c>true</c> [with overlapping].</param>
        public UpdateHostBuilder(HostEnvironment environment, HostSettings settings, string version,
			bool withOverlapping = false)
		{
			Environment = environment;
			Settings = settings;
			WithOverlapping = withOverlapping;
			var currentService = GetLastWmiServiceInfo(settings.Name, $"{settings.Name}-{version}");

			var OldSettings = new WindowsHostSettings
			{
				Name = Convert.ToString(currentService["Name"].Value),
			};

			_stopOldHostBuilder = new StopBuilder(Environment, OldSettings);
			_startOldHostBuilder = new StartBuilder(_stopOldHostBuilder);
			_uninstallOldHostBuilder = new UninstallBuilder(Environment, OldSettings);
			_installAndStartNewHostBuilder = new InstallAndStartHostBuilder(Environment, Settings, version);
			_stopAndUninstallNewHostBuilder = new StopAndUninstallHostBuilder(Environment, Settings, version);
		}

        /// <summary>
        /// Gets the last WMI service information.
        /// </summary>
        /// <param name="serviceNamePattern">The service name pattern.</param>
        /// <param name="currentName">Name of the current.</param>
        /// <returns></returns>
        private static PropertyDataCollection GetLastWmiServiceInfo(string serviceNamePattern, string currentName)
		{
			var searcher =
				new ManagementObjectSearcher(
					$@"SELECT * FROM Win32_Service WHERE (Name='{serviceNamePattern}' or Name like '%{serviceNamePattern}[^0-9a-z]%' and startmode!='disabled') and Name != '{currentName}'");
			var collection = searcher.Get();
			if (collection!=null && collection.Count == 0)
			{
				return null;
			}

			var managementBaseObject = collection.Cast<ManagementBaseObject>().Last();
            if (managementBaseObject != null)
            {
                return managementBaseObject.Properties;
            }
            return null;
		}

        /// <summary>
        /// Builds the specified service builder.
        /// </summary>
        /// <param name="serviceBuilder">The service builder.</param>
        /// <returns></returns>
        public Host Build(ServiceBuilder serviceBuilder)
		{
			return new UpdateHost(_installAndStartNewHostBuilder.Build(serviceBuilder),
				_stopOldHostBuilder.Build(serviceBuilder),
				_uninstallOldHostBuilder.Build(serviceBuilder),
				_stopAndUninstallNewHostBuilder.Build(serviceBuilder),
				_startOldHostBuilder.Build(serviceBuilder), WithOverlapping);
		}

        /// <summary>
        /// Matches the specified callback.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback">The callback.</param>
        public void Match<T>(Action<T> callback) where T : class, HostBuilder
		{
			_installAndStartNewHostBuilder.Match(callback);
			_stopOldHostBuilder.Match(callback);
			_uninstallOldHostBuilder.Match(callback);
			_stopAndUninstallNewHostBuilder.Match(callback);
			_startOldHostBuilder.Match(callback);
		}

		private sealed class UpdateHost : Host
		{
            /// <summary>
            /// The stop old host
            /// </summary>
            private readonly Host _stopOldHost;

            /// <summary>
            /// The start old host
            /// </summary>
            private readonly Host _startOldHost;

            /// <summary>
            /// The uninstall old host
            /// </summary>
            private readonly Host _uninstallOldHost;

            /// <summary>
            /// The install and start new host
            /// </summary>
            private readonly Host _installAndStartNewHost;

            /// <summary>
            /// The stop and uninstall new host
            /// </summary>
            private readonly Host _stopAndUninstallNewHost;

            /// <summary>
            /// The with overlapping
            /// </summary>
            private readonly bool _withOverlapping;

            /// <summary>
            /// Initializes a new instance of the <see cref="UpdateHost"/> class.
            /// </summary>
            /// <param name="installAndStartNewHost">The install and start new host.</param>
            /// <param name="stopOldHost">The stop old host.</param>
            /// <param name="uninstallOldHost">The uninstall old host.</param>
            /// <param name="stopAndUninstallNewHost">The stop and uninstall new host.</param>
            /// <param name="startOldHost">The start old host.</param>
            /// <param name="withOverlapping">if set to <c>true</c> [with overlapping].</param>
            public UpdateHost(Host installAndStartNewHost, Host stopOldHost, Host uninstallOldHost,
				Host stopAndUninstallNewHost, Host startOldHost, bool withOverlapping = false)
			{
				_installAndStartNewHost = installAndStartNewHost;
				_stopOldHost = stopOldHost;
				_uninstallOldHost = uninstallOldHost;
				_stopAndUninstallNewHost = stopAndUninstallNewHost;
				_startOldHost = startOldHost;
				_withOverlapping = withOverlapping;
			}

            /// <summary>
            /// Runs the configured host
            /// </summary>
            /// <returns></returns>
            public TopshelfExitCode Run()
			{
				Log.InfoFormat("Update {0} Overlapping", _withOverlapping ? "with" : "without");
				var exitCode = TopshelfExitCode.Ok;
				if (!_withOverlapping)
				{
					exitCode = _stopOldHost.Run();
					Log.InfoFormat("Service was self-stopped");
				}
				if (exitCode == TopshelfExitCode.Ok)
				{
					exitCode = _installAndStartNewHost.Run();
					if (exitCode == TopshelfExitCode.Ok)
					{
						Log.InfoFormat("Started new version");
						if (_withOverlapping)
							_stopOldHost.Run();
						exitCode = _uninstallOldHost.Run();
						Log.InfoFormat("The update has been successfully completed.");
					}
					else
					{
						Log.InfoFormat("Not started new version.");
						if (!_withOverlapping)
							exitCode = _startOldHost.Run();
						exitCode = _stopAndUninstallNewHost.Run();
						Log.WarnFormat("During the update failed and was rolled back.");
					}
				}

				return exitCode;
			}
		}
	}
}