// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Topshelf.Configuration.Dsl
{
    using System;
    using System.Collections.Generic;
    using System.ServiceProcess;
    using Model;

    public class RunnerConfigurator :
        IRunnerConfigurator
    {
        readonly IList<Func<IServiceController>> _serviceConfigurators;
        readonly WinServiceSettings _winServiceSettings;
        Action<IServiceCoordinator> _afterStop = c => { };
        Action<IServiceCoordinator> _beforeStart = c => { };
        Action<IServiceCoordinator> _beforeStartingServices = c => { };
        Credentials _credentials;
        bool _disposed;


        /// <summary>
        /// Initializes a new instance of the <see cref="RunnerConfigurator"/> class.
        /// </summary>
        RunnerConfigurator()
        {
            _winServiceSettings = new WinServiceSettings();
            _credentials = Credentials.LocalSystem;
            _serviceConfigurators = new List<Func<IServiceController>>();
        }

        #region WinServiceSettings

        /// <summary>
        /// Sets the display name of the service within the Service Control Manager.
        /// </summary>
        /// <param name="displayName">The display name of the service.</param>
        public void SetDisplayName(string displayName)
        {
            _winServiceSettings.DisplayName = displayName;
        }

        /// <summary>
        /// Sets the name of the service.
        /// </summary>
        /// <remarks>
        /// This is the name of the service that will be used when starting or stopping the service from the
        /// commandline.
        /// </remarks>
        /// <example>
        /// <para>For the following configuration:</para>
        /// <code><![CDATA[IRunConfiguration config = RunnerConfiguration.New(c =>
        /// {
        ///		c.SetDisplayName("MyService");
        ///	});
        /// ]]>
        /// </code>
        /// <para>the service will be started with the following:</para>
        /// <code>
        /// net start MyService
        /// </code>
        /// </example>
        /// <param name="serviceName">The name of the service.</param>
        public void SetServiceName(string serviceName)
        {
            _winServiceSettings.ServiceName = serviceName;
        }

        /// <summary>
        /// Sets the description of the service as it will appear in the Service Control Manager.
        /// </summary>
        /// <param name="description">The description of the service.</param>
        public void SetDescription(string description)
        {
            _winServiceSettings.Description = description;
        }

        /// <summary>
        /// We set the service to start automatically by default. This sets the service to manual instead.
        /// </summary>
        public void DoNotStartAutomatically()
        {
            _winServiceSettings.StartMode = ServiceStartMode.Manual;
        }

        /// <summary>
        /// Registers a dependency on a named Windows service that must start before this service.
        /// </summary>
        /// <param name="serviceName">The name of the service that must start before this service.</param>
        public void DependsOn(string serviceName)
        {
            _winServiceSettings.Dependencies.Add(serviceName);
        }

        /// <summary>
        /// Registers a dependency on the Microsoft Message Queue service.
        /// </summary>
        public void DependencyOnMsmq()
        {
            DependsOn(KnownServiceNames.Msmq);
        }

        /// <summary>
        /// Registers a dependency on the Microsoft SQL Server service.
        /// </summary>
        public void DependencyOnMsSql()
        {
            DependsOn(KnownServiceNames.SqlServer);
        }

        /// <summary>
        /// Registers a dependency on the Event Log service.
        /// </summary>
        public void DependencyOnEventLog()
        {
            DependsOn(KnownServiceNames.EventLog);
        }

        /// <summary>
        /// Registers a dependency on the Internet Information Server service.
        /// </summary>
        public void DependencyOnIis()
        {
            DependsOn(KnownServiceNames.IIS);
        }

        #endregion

        #region IRunnerConfigurator Members

        /// <summary>
        /// Configures a service using the specified configuration action or set of configuration actions.
        /// </summary>
        /// <typeparam name="TService">The type of the service that will be configured.</typeparam>
        /// <param name="name">The name used to identify the service</param>
        /// <param name="action">The configuration action or set of configuration actions that will be performed.</param>
        public void ConfigureService<TService>(Action<IServiceConfigurator<TService>> action) where TService : class
        {
            var configurator = new ServiceConfigurator<TService>();
            _serviceConfigurators.Add(() =>
            {
                action(configurator);
                return configurator.Create();
            });
        }

        /// <summary>
        /// Configures an isolated service using the specified configuration action or set of configuration actions.
        /// </summary>
        /// <typeparam name="TService">The type of the isolated service that will be configured.</typeparam>
        /// <param name="name">The name used to identify the service</param>
        /// <param name="action">The configuration action or set of configuration actions that will be performed.</param>
        public void ConfigureServiceInIsolation<TService>(Action<IIsolatedServiceConfigurator<TService>> action) where TService : class
        {
            var configurator = new IsolatedServiceConfigurator<TService>();
            _serviceConfigurators.Add(() =>
            {
                action(configurator);
                return configurator.Create();
            });
        }

        /// <summary>
        /// Configures a service using a completely separate appdomain and a completely different private bin folder
        /// </summary>
        /// <param name="action">The configuration action or set of configuration actions that will be performed.</param>
        public void ConfigureServiceInShelving(Action<IShelvedServiceConfigurator> action)
        {
            var configurator = new ShelvedServiceConfigurator();
            _serviceConfigurators.Add(()=>
            {
                action(configurator);
                return configurator.Create();
            });
        }

        public void BeforeStartingServices(Action<IServiceCoordinator> action)
        {
            _beforeStartingServices = action;
        }

        /// <summary>
        /// Defines an action or a set of actions to perform before the service starts.
        /// </summary>
        /// <remarks>
        /// This is the best place to set up, for example, any IoC containers.
        /// </remarks>
        /// <param name="action">The action or actions that will be performed before the service starts.</param>
        public void BeforeStartingTheHost(Action<IServiceCoordinator> action)
        {
            _beforeStart = action;
        }

        /// <summary>
        /// Defines an action or a set of actions to perform after the service stops.
        /// </summary>
        /// <param name="action">The action or actions that will be performed after the service stops.</param>
        public void AfterStoppingTheHost(Action<IServiceCoordinator> action)
        {
            _afterStop = action;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// Configures a service using the default configuration.
        /// </summary>
        /// <typeparam name="TService">The type of the service that will be configured.</typeparam>
        public void ConfigureService<TService>() where TService : class
        {
            ConfigureService<TService>(x => { });
        }

        /// <summary>
        /// Configures an isolated service using the default configuration.
        /// </summary>
        /// <typeparam name="TService">The type of the isolated service that will be configured.</typeparam>
        public void ConfigureServiceInIsolation<TService>()
            where TService : MarshalByRefObject
        {
            ConfigureServiceInIsolation<TService>(x => { });
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
        void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _serviceConfigurators.Clear();
            }
            _disposed = true;
        }

        RunConfiguration Create()
        {
            var serviceCoordinator = new ServiceCoordinator(_beforeStartingServices, _beforeStart, _afterStop);
            serviceCoordinator.RegisterServices(_serviceConfigurators);
            _winServiceSettings.Credentials = _credentials;
            var cfg = new RunConfiguration
                      {
                          WinServiceSettings = _winServiceSettings,
                          Coordinator = serviceCoordinator
                      };

            return cfg;
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="RunnerConfigurator"/> is reclaimed by garbage collection.
        /// </summary>	
        ~RunnerConfigurator()
        {
            Dispose(false);
        }

        public static RunConfiguration New(Action<IRunnerConfigurator> action)
        {
            using (var configurator = new RunnerConfigurator())
            {
                action(configurator);
                return configurator.Create();
            }
        }

        #region Credentials

        /// <summary>
        /// The application will run with the Local System credentials.
        /// </summary>
        public void RunAsLocalSystem()
        {
            _credentials = Credentials.LocalSystem;
        }

        /// <summary>
        /// The application will run with the Local Service credentials.
        /// </summary>
        public void RunAsLocalService()
        {
            _credentials = Credentials.LocalService;
        }

        /// <summary>
        /// The application will run with the Network Service credentials.
        /// </summary>
        public void RunAsNetworkService()
        {
            _credentials = Credentials.NetworkService;
        }

        /// <summary>
        /// The application will run with the Local System credentials, with the ability to interact with the desktop.
        /// </summary>
        public void RunAsFromInteractive()
        {
            _credentials = Credentials.Interactive;
        }

        /// <summary>
        /// The application will run with the credentials specified in the commandline arguments.
        /// </summary>
        /// <example>
        /// The commandline arguments should be in the format:
        /// <code><![CDATA[MyApplication.exe /credentials:username#password]]>
        /// 	</code>
        /// This means that <c>#</c> will not be a valid character to use in the password.
        /// </example>
        public void RunAsFromCommandLine()
        {
            throw new NotImplementedException("soon though");
        }

        /// <summary>
        /// The application will run with the specified credentials.
        /// </summary>
        /// <param name="username">The name of the user that the application will run as.</param>
        /// <param name="password">The password of the user that the application will run as.</param>
        /// <remarks>
        /// If the application is a Windows service, then ensure that the user account has sufficient
        /// privileges to install and run services.
        /// </remarks>
        public void RunAs(string username, string password)
        {
            _credentials = Credentials.Custom(username, password);
        }

        #endregion
    }
}