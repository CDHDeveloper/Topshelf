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
namespace Topshelf.Specs
{
    using System;
    using System.Collections.Generic;
    using Model;
    using NUnit.Framework;
    using TestObject;

    [TestFixture]
    public class ServiceCoordinator_Specs
    {
        #region Setup/Teardown

        [SetUp]
        public void EstablishContext()
        {
            _service = new TestService();
            _service2 = new TestService2();


            _serviceCoordinator = new ServiceCoordinator(x => { }, x => { }, x => { });
            IList<Func<IServiceController>> services = new List<Func<IServiceController>>
                                                       {
                                                           () => new ServiceController<TestService>
                                                                 {
                                                                     Name = "test",
                                                                     BuildService = s => _service,
                                                                     StartAction = x => x.Start(),
                                                                     StopAction = x => x.Stop(),
                                                                     ContinueAction = x => x.Continue(),
                                                                     PauseAction = x => x.Pause()
                                                                 },
                                                           () => new ServiceController<TestService2>
                                                                 {
                                                                     Name = "test2",
                                                                     BuildService = s => _service2,
                                                                     StartAction = x => x.Start(),
                                                                     StopAction = x => x.Stop(),
                                                                     ContinueAction = x => x.Continue(),
                                                                     PauseAction = x => x.Pause()
                                                                 }
                                                       };

            _serviceCoordinator.RegisterServices(services);
        }

        #endregion

        private TestService _service;
        private TestService2 _service2;
        private ServiceCoordinator _serviceCoordinator;

        [Test]
        public void Continueing_the_coordintaro_should_continue_services()
        {
            _serviceCoordinator.Start();
            _serviceCoordinator.Pause();
            _serviceCoordinator.Continue();

            _service.HasBeenStarted
                .ShouldBeTrue();
            _service.HasBeenContinued
                .ShouldBeTrue();
            _service.Started
                .ShouldBeTrue();
        }

        [Test]
        public void Pausing_the_coordinator_should_pause_services()
        {
            _serviceCoordinator.Start();
            _serviceCoordinator.Pause();

            _service.HasBeenStarted
                .ShouldBeTrue();
            _service.Paused
                .ShouldBeTrue();
        }

        [Test]
        public void Starting_the_coordinator_should_start_all_services()
        {
            _serviceCoordinator.Start();
            _service.HasBeenStarted
                .ShouldBeTrue();
            _service2.HasBeenStarted
                .ShouldBeTrue();
        }

        [Test]
        public void Stopping_the_coordinator_should_stop_all_services()
        {
            _serviceCoordinator.Start();
            _serviceCoordinator.Stop();

            _service.HasBeenStarted
                .ShouldBeTrue();
            _service.Stopped
                .ShouldBeTrue();

            _service2.HasBeenStarted
                .ShouldBeTrue();
            _service2.Stopped
                .ShouldBeTrue();
        }

        [Test]
        public void Unstarted_coordinator_should_not_start_services()
        {
            _service.HasBeenStarted
                .ShouldBeFalse();
        }

        [Test, Ignore]
        public void You_cant_continue_if_you_arent_currently_paused()
        {
            Assert.Fail();
        }

        [Test, Ignore]
        public void You_cant_pause_if_you_havent_started()
        {
            Assert.Fail();
        }
    }
}