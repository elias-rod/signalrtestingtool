namespace SignalRTest.Interfaces
{
    public interface ITestConfiguration
    {
        int SignalRConcurrentConnections { get; set; }

        string TestName { get; set; }

        string SignalREndpoint { get; set; }

        string SignalRToken { get; set; }

        public int ListeningWaitingMs { get; set; }
    }
}
