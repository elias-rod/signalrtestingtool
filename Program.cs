using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SignalRTest.Interfaces;
using SignalRTest.Models;
using SignalRTest.Services;

namespace SignalRTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton<ISignalRService, SignalRService>();
                    services.AddSingleton<ITestConfiguration, TestConfiguration>();
                });
    }
}
