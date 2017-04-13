using System;
using System.ServiceProcess;
using Topshelf.Builders;
using Topshelf.Logging;
using Topshelf.Runtime;
using Topshelf.Runtime.Windows;

namespace Topshelf.Squirrel.Updater.Builders
{
	public sealed class InstallAndStartHostBuilder : HostBuilder
	{

        #region Logger
        
        private static readonly LogWriter Log = HostLogger.Get(typeof(InstallAndStartHostBuilder));

        #endregion

        /// <summary>
        /// The install builder
        /// </summary>
        private readonly InstallBuilder _installBuilder;

        /// <summary>
        /// The start builder
        /// </summary>
        private readonly StartBuilder _startBuilder;

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
        /// Initializes a new instance of the <see cref="InstallAndStartHostBuilder"/> class.
        /// </summary>
        /// <param name="environment">The environment.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="version">The version.</param>
        public InstallAndStartHostBuilder(HostEnvironment environment, HostSettings settings, string version)
		{
			Environment = environment;
			Settings = new WindowsHostSettings
			{
				Name = $"{settings.Name}-{version}",
				DisplayName = $"{settings.DisplayName} ({version})",
				Description = settings.Description,
				InstanceName = settings.InstanceName,
				CanPauseAndContinue = settings.CanPauseAndContinue,
				CanSessionChanged = settings.CanSessionChanged,
				CanShutdown = settings.CanShutdown,
                CanHandlePowerEvent = settings.CanHandlePowerEvent,
                ExceptionCallback = settings.ExceptionCallback,
                StartTimeOut = settings.StartTimeOut,
                StopTimeOut = settings.StopTimeOut
            };

			_installBuilder = new InstallBuilder(Environment, Settings);
			_installBuilder.Sudo();
			_startBuilder = new StartBuilder(_installBuilder);
		}

        /// <summary>
        /// Builds the specified service builder.
        /// </summary>
        /// <param name="serviceBuilder">The service builder.</param>
        /// <returns></returns>
        public Topshelf.Host Build(ServiceBuilder serviceBuilder)
		{
			return _startBuilder.Build(serviceBuilder);
		}

        /// <summary>
        /// Matches the specified callback.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback">The callback.</param>
        public void Match<T>(Action<T> callback) where T : class, HostBuilder
		{
			_installBuilder.Match(callback);
			_startBuilder.Match(callback);
		}
	}
}