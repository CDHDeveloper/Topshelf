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

    public class ServiceControllerProxy :
        MarshalByRefObject,
        IServiceController
    {
        readonly IServiceControllerOf<object> _target;


        public ServiceControllerProxy(Type type)
        {
            var targetType = typeof(IsolatedServiceControllerWrapper<>).MakeGenericType(type);
            _target = (IServiceControllerOf<object>) Activator.CreateInstance(targetType);
            Actions = new SerializableActions<object>();
        }

        public SerializableActions<object> Actions { get; private set; }

        #region IServiceController Members

        public void Initialize()
        {
            
        }

        public void Dispose()
        {
            _target.Dispose();
        }

        public Type ServiceType
        {
            get { return _target.ServiceType; }
        }

        public string Name
        {
            get { return _target.Name; }
            set { _target.Name = value; }
        }

        public ServiceState State
        {
            get { return _target.State; }
        }

        public void Start()
        {
            _target.Start();
        }

        public void Stop()
        {
            _target.Stop();
        }

        public void Pause()
        {
            _target.Pause();
        }

        public void Continue()
        {
            _target.Continue();
        }

        public ServiceBuilder BuildService
        {
            get { return _target.BuildService; }
        }

        #endregion
    }
}