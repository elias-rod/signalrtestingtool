using System;
using Microsoft.AspNetCore.SignalR.Client;

namespace SignalRTest.Interfaces
{
    public interface ISignalRService
    {
        HubConnection GetHubConnection(Action<object> messageHandler);
    }
}
