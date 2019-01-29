using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using Topshelf.Builders;
using Topshelf.Logging;
using Topshelf.Runtime;
using Topshelf.Runtime.Windows;

namespace Topshelf.Squirrel.Updater.Builders
{
    public sealed class StopAndUninstallHostBuilder : HostBuilder
    {
        #region Logger

        private static readonly LogWriter Log = HostLogger.Get(typeof(StopAndUninstallHostBuilder));

        #endregion

        /// <summary>
        /// The stop builder
        /// </summary>
        private readonly StopBuilder _stopBuilder;

        /// <summary>
        /// The uninstall builder
        /// </summary>
        private readonly UninstallBuilder _uninstallBuilder;

        /// <summary>
        /// The process identifier
        /// </summary>
        private readonly int _processId;

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
        /// Initializes a new instance of the <see cref="StopAndUninstallHostBuilder"/> class.
        /// </summary>
        /// <param name="environment">The environment.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="version">The version.</param>
        public StopAndUninstallHostBuilder(HostEnvironment environment, HostSettings settings, string version = null)
		{
			Environment = environment;

			var serviceName = settings.Name;
			if (version != null)
			{
				serviceName = $"{serviceName.Split('-')[0]}-{version}";
			}

			var serviceInfo = GetWmiServiceInfo(serviceName);
			if (serviceInfo != null)
			{
				serviceName = Convert.ToString(serviceInfo["Name"].Value);
				_processId = Convert.ToInt32(serviceInfo["ProcessId"].Value);
			}

			Settings = new WindowsHostSettings
			{
				Name = serviceName,

				// squirrel hook wait only 15 seconds
				// so we can't wait for service stop more that 5 seconds
				StopTimeOut = TimeSpan.FromSeconds(5),
			};

			_stopBuilder = new StopBuilder(Environment, Settings);
			_uninstallBuilder = new UninstallBuilder(Environment, Settings);
			_uninstallBuilder.Sudo();
		}

        /// <summary>
        /// Builds the specified service builder.
        /// </summary>
        /// <param name="serviceBuilder">The service builder.</param>
        /// <returns></returns>
        public Host Build(ServiceBuilder serviceBuilder)
		{
			return new StopAndUninstallHost(_stopBuilder.Build(serviceBuilder), _uninstallBuilder.Build(serviceBuilder), _processId);
		}

        /// <summary>
        /// Matches the specified callback.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback">The callback.</param>
        public void Match<T>(Action<T> callback) where T : class, HostBuilder
		{
			_stopBuilder.Match(callback);
			_uninstallBuilder.Match(callback);
		}

        /// <summary>
        /// Gets the WMI service information.
        /// </summary>
        /// <param name="serviceNamePattern">The service name pattern.</param>
        /// <returns></returns>
        private static PropertyDataCollection GetWmiServiceInfo(string serviceNamePattern)
		{
			var searcher = new ManagementObjectSearcher($@"SELECT * FROM Win32_Service WHERE Name='{serviceNamePattern}' or Name like '%{serviceNamePattern}[^0-9a-z]%' and startmode!='disabled'");
			var collection = searcher.Get();
			if (collection.Count == 0)
			{
				return null;
			}

			var managementBaseObject = collection.Cast<ManagementBaseObject>().Last();
			return managementBaseObject.Properties;
		}

		private sealed class StopAndUninstallHost : Host
		{
            /// <summary>
            /// The stop host
            /// </summary>
            private readonly Host _stopHost;

            /// <summary>
            /// The uninstall host
            /// </summary>
            private readonly Host _uninstallHost;

            /// <summary>
            /// The process identifier
            /// </summary>
            private readonly int _processId;

            /// <summary>
            /// Initializes a new instance of the <see cref="StopAndUninstallHost"/> class.
            /// </summary>
            /// <param name="stopHost">The stop host.</param>
            /// <param name="uninstallHost">The uninstall host.</param>
            /// <param name="processId">The process identifier.</param>
            public StopAndUninstallHost(Host stopHost, Host uninstallHost, int processId)
			{
				_stopHost = stopHost;
				_uninstallHost = uninstallHost;
				_processId = processId;
			}

            /// <summary>
            /// Runs the configured host
            /// </summary>
            /// <returns></returns>
            public TopshelfExitCode Run()
			{
				var exitCode = _stopHost.Run();

				if (exitCode == TopshelfExitCode.ServiceNotInstalled)
				{
					return TopshelfExitCode.Ok;
				}

				if (exitCode == TopshelfExitCode.ServiceControlRequestFailed)
				{
					Process.GetProcessById(_processId).Kill();
					exitCode = TopshelfExitCode.Ok;
				}

				if (exitCode == TopshelfExitCode.Ok)
				{
					exitCode = _uninstallHost.Run();
				}


                return exitCode;
			}
		}
	}
}
