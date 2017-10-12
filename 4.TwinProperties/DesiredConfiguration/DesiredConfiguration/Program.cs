using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.Devices;
using System.Threading;
using Newtonsoft.Json;

namespace DesiredConfiguration
{
    class Program
    {
        static RegistryManager registryManager;
        static string connectionString = "{iot hub connection string}";
        static void Main(string[] args)
        {
            registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            SetDesiredConfigurationAndQuery();
            Console.WriteLine("Press any key to quit.");
            Console.ReadLine();
        }

        static private async Task SetDesiredConfigurationAndQuery()
        {
            var twin = await registryManager.GetTwinAsync("dmTest");
            var patch = new
            {
                properties = new
                {
                    desired = new
                    {
                        telemetryConfig = new
                        {
                            configId = Guid.NewGuid().ToString(),
                            sendFrequency = "5m"
                        }
                    }
                }
            };

            await registryManager.UpdateTwinAsync(twin.DeviceId, JsonConvert.SerializeObject(patch), twin.ETag);
            Console.WriteLine("Updated desired configuration");

            while (true)
            {
                var query = registryManager.CreateQuery("SELECT * FROM devices WHERE deviceId = 'dmTest'");
                var results = await query.GetNextAsTwinAsync();
                foreach (var result in results)
                {
                    Console.WriteLine("Config report for: {0}", result.DeviceId);
                    Console.WriteLine("Desired telemetryConfig: {0}", JsonConvert.SerializeObject(result.Properties.Desired["telemetryConfig"], Formatting.Indented));
                    Console.WriteLine("Reported telemetryConfig: {0}", JsonConvert.SerializeObject(result.Properties.Reported["telemetryConfig"], Formatting.Indented));
                    Console.WriteLine();
                }
                Thread.Sleep(10000);
            }
        }
    }
}
