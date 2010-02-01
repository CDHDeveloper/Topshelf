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
namespace Topshelf.Model.Isolated
{
    using System;
    using System.Diagnostics;
    using ApplicationDomain;

    [Serializable]
    [DebuggerDisplay("Isolated Service({Name}) - {State}")]
    public class FacadeToIsolatedServiceController<TService> :
        IServiceController
        where TService : class
    {
        private AppDomainBundle _domain;
        private IsolatedServiceControllerProxy _remoteIsolatedServiceController;
        private SerializableActions<TService> _delegates = new SerializableActions<TService>();

        public void Initialize()
        {
            
        }
        public void Start()
        {
            _domain = AppDomainFactory.CreateNewAppDomain(new IsolatedServiceInfo()
                                                          {
                                                              Args = this.Args,
                                                              ConfigureArgsAction = this.ConfigureArgsAction,
                                                              Name = typeof(TService).AssemblyQualifiedName,
                                                              PathToConfigurationFile = this.PathToConfigurationFile
                                                          });

            var type = typeof(IsolatedServiceControllerProxy);
            _remoteIsolatedServiceController = (IsolatedServiceControllerProxy)_domain.Domain.CreateInstanceAndUnwrap(type.Assembly.GetName().FullName, type.FullName, true, 0, null, new object[]{ typeof(TService)}, null, null, null);

            if (_remoteIsolatedServiceController == null)
                throw new ApplicationException("Unable to create service proxy for " + typeof(TService).Name);

            _remoteIsolatedServiceController.Actions.StartAction = ConvertForUseWithAnObject(_delegates.StartAction);
            _remoteIsolatedServiceController.Actions.StopAction = ConvertForUseWithAnObject(_delegates.StopAction);
            _remoteIsolatedServiceController.Actions.PauseAction = ConvertForUseWithAnObject(_delegates.PauseAction);
            _remoteIsolatedServiceController.Actions.ContinueAction = ConvertForUseWithAnObject(_delegates.ContinueAction);
            _remoteIsolatedServiceController.Actions.BuildServiceAction = _delegates.BuildServiceAction;
            
            _remoteIsolatedServiceController.Name = Name;

            _remoteIsolatedServiceController.Start();
        }

        private static Action<object> ConvertForUseWithAnObject(Action<TService> action)
        {
            return o => action((TService) o);
        }

        //figure out a way to get rid of these?
        public SerializableActions<TService> Delegates { get { return _delegates; } }
        public Action<TService> StartAction { set { _delegates.StartAction = value; } }
        public Action<TService> StopAction { set { _delegates.StopAction = value; } }
        public Action<TService> PauseAction { set { _delegates.PauseAction = value; } }
        public Action<TService> ContinueAction { set { _delegates.ContinueAction = value; } }
        public ServiceBuilder BuildAction { set { _delegates.BuildServiceAction = ()=>value; } }
        public string Name { get; set; }
        public string PathToConfigurationFile { get; set; }
        public string[] Args { get; set; }
        public Func<AppDomainInitializer> ConfigureArgsAction { get; set; }

        public void Stop()
        {
            _remoteIsolatedServiceController.IfNotNull(x => x.Stop());

            _domain.Dispose();
        }

        public void Pause()
        {
            _remoteIsolatedServiceController.IfNotNull(x => x.Pause());
        }

        public void Continue()
        {
            _remoteIsolatedServiceController.IfNotNull(x => x.Continue());
        }

        public ServiceBuilder BuildService
        {
            get
            {
                return _remoteIsolatedServiceController.IfNotNull<ServiceBuilder>(x => x.BuildService, s => new object());
            }
        }

        public void Dispose()
        {
            //do nothing?
        }

        public Type ServiceType
        {
            get { return _remoteIsolatedServiceController.IfNotNull(x => x.ServiceType, typeof(object)); }
        }

        public ServiceState State
        {
            get { return _remoteIsolatedServiceController.IfNotNull(x => x.State, ServiceState.Stopped); }
        }
    }
}