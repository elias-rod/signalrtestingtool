using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SignalRTest.Interfaces;

namespace SignalRTest.Services
{
    public class SignalRService : ISignalRService
    {
        private readonly ITestConfiguration _testConfiguration;

        public SignalRService(ITestConfiguration testConfiguration)
        {
            _testConfiguration = testConfiguration;
        }

        public HubConnection GetHubConnection(Action<object> messageHandler)
        { 
            var topic = "myTopic";
            var connection = new HubConnectionBuilder()
                .WithUrl(_testConfiguration.SignalREndpoint, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_testConfiguration.SignalRToken);
                })
                .WithAutomaticReconnect()
                .Build();

            connection.On(topic, messageHandler);

            return connection;
        }
    }
}
