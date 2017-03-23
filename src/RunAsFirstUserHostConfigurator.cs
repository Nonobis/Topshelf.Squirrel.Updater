﻿using log4net;
using SimpleHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.ServiceProcess.Design;
using Topshelf.Builders;
using Topshelf.Configurators;
using Topshelf.HostConfigurators;

namespace Topshelf.Squirrel.Windows
{
	public class RunAsFirstUserHostConfigurator : HostBuilderConfigurator
	{

        #region Definition du logger

        /// <summary>
        /// Logger Log4Net
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(RunAsFirstUserHostConfigurator));

        #endregion

        /// <summary>
        /// The credentials
        /// </summary>
        private const string credentials = "credentials.txt";

        /// <summary>
        /// Gets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        public string Password { get; private set; }

        /// <summary>
        /// Gets the username.
        /// </summary>
        /// <value>
        /// The username.
        /// </value>
        public string Username { get; private set; }

        /// <summary>
        /// Configures the specified builder.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">builder</exception>
        public HostBuilder Configure(HostBuilder builder)
		{
			if (builder == null)
				throw new ArgumentNullException("builder");

			builder.Match<InstallBuilder>(x =>
           {
               bool valid = false;
               var path = new FileInfo(AssemblyHelper.AssemblyDirectory).Directory.Parent.FullName;
               var filename = Path.Combine(path, credentials);
               if (File.Exists(filename))
               {
                   try
                   {
                       var credlines = File.ReadAllLines(filename);
                       Username = credlines[0];
                       Password = credlines[1];
                       valid = CheckCredentials(Username, Password);
                   }
                   catch (Exception ex)
                   {
                       Log.ErrorFormat("Reading error: {0}", ex);
                   }
               }
               while (!valid)
               {
                   using (ServiceInstallerDialog serviceInstallerDialog = new ServiceInstallerDialog())
                   {
                       serviceInstallerDialog.Username = Username;
                       serviceInstallerDialog.ShowInTaskbar = true;
                       serviceInstallerDialog.ShowDialog();
                       switch (serviceInstallerDialog.Result)
                       {
                           case ServiceInstallerDialogResult.OK:
                               Username = serviceInstallerDialog.Username;
                               Password = serviceInstallerDialog.Password;
                               valid = CheckCredentials(Username, Password);
                               if (valid)
                               {
                                   File.WriteAllLines(filename, new[] { Username, Password });
                               }
                               break;
                           case ServiceInstallerDialogResult.Canceled:
                               throw new InvalidOperationException("UserCanceledInstall");
                       }
                   }
               }
               x.RunAs(Username, Password, ServiceAccount.User);
           });
			return builder;
		}

        /// <summary>
        /// Checks the credentials.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        private bool CheckCredentials(string username, string password)
		{
			try
			{
				if (username.StartsWith(@".\", StringComparison.Ordinal))
				{
					using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
					{
						return context.ValidateCredentials(username.Remove(0, 2), password);
					}
				}
				using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
				{
					return context.ValidateCredentials(username, password);
				}
			}
			catch (Exception ex)
			{
				Log.ErrorFormat("Exception: {0}", ex);
				return false;
			}
		}

        /// <summary>
        /// Validates this instance.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ValidateResult> Validate()
		{
			yield return this.Success("All ok!");
		}
	}
}