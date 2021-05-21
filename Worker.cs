using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SignalRTest.Interfaces;

namespace SignalRTest
{
    public class ClientWithData
    {
        public ClientWithData(int clientNumber, bool hasData)
        {
            ClientNumber = clientNumber;
            HasData = hasData;
        }

        public int ClientNumber { get; set; }

        public bool HasData { get; set; }
    }
    public class Worker : IHostedService
    {
        private readonly int _connectionNumber;
        private readonly ISignalRService _signalRService;
        private readonly ILogger<Worker> _logger;
        private object _lockClientsConnected;
        private object _lockClientsReceivedData;
        private object _lockAddConnection;
        private readonly ICollection<HubConnection> _hubsConnections;
        private readonly ITestConfiguration _testConfiguration;

        public Worker(ISignalRService signalRService, ILogger<Worker> logger, ITestConfiguration testConfiguration)
        {
            _signalRService = signalRService;
            _connectionNumber = testConfiguration.SignalRConcurrentConnections;
            _logger = logger;
            _hubsConnections = new List<HubConnection>();
            _lockClientsConnected = new object();
            _lockClientsReceivedData = new object();
            _lockAddConnection = new object();
            _testConfiguration = testConfiguration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting test {_testConfiguration.TestName}");
            var numberClientsConnected = 0;
            var numberClientsReceivedData = 0;
            var clientsReceivedData = new List<ClientWithData>();
            var connectingClients = new List<Task>();

            Action incrementClientsConnected = () =>
            {
                lock (_lockClientsConnected)
                {
                    numberClientsConnected++;
                }
            };

            for (int i = 0; i < _connectionNumber; i++)
            {
                var clientNumber = i;
                clientsReceivedData.Add(new ClientWithData(i, false));
                Action<object> incrementClientsReceivedData = (object data) =>
                {
                    lock (_lockClientsReceivedData)
                    {
                        numberClientsReceivedData++;
                        clientsReceivedData[clientNumber].HasData = true;
                    }
                    _logger.LogInformation("Client " + clientNumber + " received data at {time}.", DateTime.UtcNow);
                };
                var hubConnection = _signalRService.GetHubConnection(incrementClientsReceivedData);

                AddHubConnection(hubConnection);

                connectingClients.Add(ConnectAndCount(incrementClientsConnected, i, hubConnection, cancellationToken));
            }

            try
            {
                await Task.WhenAll(connectingClients);
            }
            catch (Exception)
            {
                MultipleTasksExceptionHandler(connectingClients);
            }


            _logger.LogInformation("{numberClientsConnected} connections stablished", numberClientsConnected);
            _logger.LogInformation("Listening...");

            _logger.LogInformation($"Waiting time {_testConfiguration.ListeningWaitingMs}");

            await Task.Delay(TimeSpan.FromMilliseconds(_testConfiguration.ListeningWaitingMs), cancellationToken);

            _logger.LogInformation("{numberClientsReceivedData} clients successfully received data within {ListeningWaitingMs} milliseconds", numberClientsReceivedData, _testConfiguration.ListeningWaitingMs);

            var clientsWithoutData = clientsReceivedData.Where(client => !client.HasData).Select(client => client.ClientNumber);

            if (clientsWithoutData.Any())
            {
                _logger.LogWarning($"These clients did not receive data: {string.Join(",", clientsWithoutData)}");
            }
            else
            {
                _logger.LogInformation($"All clients received data successfully");
            }

            _logger.LogInformation($"Test {_testConfiguration.TestName} finished");

            await DisposeConnectionsAsync (cancellationToken);
        }

        private void AddHubConnection(HubConnection signalRConnection)
        {
            lock (_lockAddConnection)
            {
                _hubsConnections.Add(signalRConnection);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await DisposeConnectionsAsync(cancellationToken);
        }

        private async Task DisposeConnectionsAsync(CancellationToken cancellationToken)
        {
            var disposingHubConnections = new List<ValueTask>();

            foreach (var hubConnection in _hubsConnections)
            {
                disposingHubConnections.Add(hubConnection.DisposeAsync());
            }

            foreach (var task in disposingHubConnections)
            {
                await task;
            }
        }

        private void MultipleTasksExceptionHandler(IEnumerable<Task> tasks)
        {
            var exceptions = tasks.Where(task => task.IsFaulted).SelectMany(x => x.Exception.InnerExceptions);

            foreach (var exception in exceptions)
            {
                _logger.LogError(exception, exception.Message);
            }
        }

        private async Task ConnectAndCount(Action incrementClientsConnected, int i, HubConnection hubConnection, CancellationToken cancellationToken, int tryCount = 0)
        {
            try
            {
                await hubConnection.StartAsync(cancellationToken);
                incrementClientsConnected();

                _logger.LogInformation($"Client {i} connected at {DateTime.UtcNow}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Client {i} failed to connect");

                //if (hubConnection.State == HubConnectionState.Disconnected && tryCount < 3)
                //{
                //    await ConnectAndCount(incrementClientsConnected, i, hubConnection, cancellationToken, ++tryCount);
                //}
                //else
                //{
                    //_logger.LogError(ex, $"Max retry count reached for client {i}");

                    throw;
                //}
            }
        }
    }
}
