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

namespace Topshelf.Specs.Configuration
{
    using System.ServiceProcess;
    using Model;
    using NUnit.Framework;
    using TestObject;
    using Topshelf.Configuration;
    using Topshelf.Configuration.Dsl;

    [TestFixture]
    public class RunnerConfigurator_Specs
    {
        private RunConfiguration _runConfiguration;

        [SetUp]
        public void EstablishContext()
        {
            TestService s1 = new TestService();
            TestService s2 = new TestService();

            _runConfiguration = (RunConfiguration)RunnerConfigurator.New(x =>
            {
                x.SetDisplayName("chris");
                x.SetServiceName("chris");
                x.SetDescription("chris's pants");

                x.ConfigureService<TestService>(c =>
                {
                    c.WhenStarted(s => s.Start());
                    c.WhenStopped(s => s.Stop());
                    c.WhenPaused(s => { });
                    c.WhenContinued(s => { });
                    c.Named("my_service");
                });

                //needs to moved to a custom area for testing
                //x.ConfigureServiceInIsolation<TestService>(c=>
                //                                                   {
                //                                                       c.WhenStarted(s => s.Start());
                //                                                       c.WhenStopped(s => s.Stop());
                //                                                       c.WhenPaused(s => { });
                //                                                       c.WhenContinued(s => { });
                //                                                       c.HowToBuildService(()=>sl);
                //                                                   });
                

                x.DoNotStartAutomatically();

                x.RunAs("dru", "pass");

                //x.UseWinFormHost<MyForm>();

                x.DependsOn("ServiceName");
                x.DependencyOnMsmq();
                x.DependencyOnMsSql();
            });
        }


        [Test]
        public void A_pretend_void_main()
        {
            string[] args = new string[0];
            RunConfiguration cfg = RunnerConfigurator.New(x => { });
            //some thing parses the args
            //Dispatch(args, serviceCoordinator);
        }

        [Test]
        public void Should_depend_on_Msmq_MsSql_and_Custom()
        {
            _runConfiguration.WinServiceSettings.Dependencies
                .ShouldContain(KnownServiceNames.Msmq);

            _runConfiguration.WinServiceSettings.Dependencies
                .ShouldContain(KnownServiceNames.SqlServer);

            _runConfiguration.WinServiceSettings.Dependencies
                .ShouldContain("ServiceName");
        }

        [Test]
        public void Names_should_be_correct()
        {
            _runConfiguration.WinServiceSettings.FullDisplayName
                .ShouldEqual("chris");

            _runConfiguration.WinServiceSettings.FullServiceName
                .ShouldEqual("chris");

            _runConfiguration.WinServiceSettings.Description
                .ShouldEqual("chris's pants");
        }

        [Test]
        public void Should_not_be_set_to_start_automatically()
        {
            _runConfiguration.WinServiceSettings.StartMode
                .ShouldEqual(ServiceStartMode.Manual);
        }

        [Test]
        public void Credentials()
        {
            _runConfiguration.WinServiceSettings.Credentials.Username
                .ShouldEqual("dru");

            _runConfiguration.WinServiceSettings.Credentials.Password
                .ShouldEqual("pass");

            _runConfiguration.WinServiceSettings.Credentials.AccountType
                .ShouldEqual(ServiceAccount.User);
        }

		[Test]
		public void when_specified_service_names_are_used_in_the_service_configuration()
		{
			const string serviceName = "service name";

			var runConfiguration = RunnerConfigurator.New(x =>
			{
				x.ConfigureService<TestService>(c => c.Named(serviceName));
			});

			var serviceInfo = runConfiguration.Coordinator.GetServiceInformation();

			serviceInfo[0].Name.ShouldEqual(serviceName);
		}

		[Test]
		public void when_not_specified_service_names_are_assigned()
		{
			var runConfiguration = RunnerConfigurator.New(x => x.ConfigureService<TestService>(c => { }));

			var serviceInfo = runConfiguration.Coordinator.GetServiceInformation();

			serviceInfo[0].Name.ShouldNotBeNull();
		}

    	[Test]
    	public void when_not_specified_automatic_service_names_should_be_unique_for_services_of_the_same_type()
     	{
    		var runConfiguration = RunnerConfigurator.New(x =>
    		{
				x.ConfigureService<TestService>(c => { });
    			x.ConfigureService<TestService>(c => { });
    		});
            
    		var serviceInfo = runConfiguration.Coordinator.GetServiceInformation();
            
			serviceInfo[0].Name.ShouldNotEqual(serviceInfo[1].Name);
     	}

        [Test]
        public void Hosted_service_configuration()
        {
            _runConfiguration.Coordinator.Start();
            _runConfiguration.Coordinator.HostedServiceCount
                .ShouldEqual(1);

            IServiceController serviceController = _runConfiguration.Coordinator.GetService("my_service");

            serviceController.Name
                .ShouldEqual("my_service");
            serviceController.State
                .ShouldEqual(ServiceState.Started);
        }
    }
}