using Microsoft.Extensions.Configuration;
using SignalRTest.Interfaces;

namespace SignalRTest.Models
{
    public class TestConfiguration : ITestConfiguration
    {
        public TestConfiguration(IConfiguration configuration)
        {
            configuration.Bind(this);
        }

        public string SignalREndpoint { get; set; }

        public string SignalRToken { get; set; }

        public int SignalRConcurrentConnections { get; set; }

        public string TestName { get; set; }

        public int ListeningWaitingMs { get; set; }
    }
}
