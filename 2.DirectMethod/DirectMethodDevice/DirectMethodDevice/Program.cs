using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectMethodDevice
{
    class Program
    {

        static string DeviceConnectionString = "HostName=<yourIotHubName>.azure-devices.net;DeviceId=<yourIotDeviceName>;SharedAccessKey=<yourIotDeviceAccessKey>";
        static DeviceClient deviceClient = null;
       
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Connecting to hub");
                deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Mqtt);

                // setup callback for "writeLine" method
                deviceClient.SetMethodHandlerAsync("writeLine", WriteLineToConsole, null).Wait();
                Console.WriteLine("Waiting for direct method call\n Press enter to exit.");
                Console.ReadLine();

                Console.WriteLine("Exiting...");

                // as a good practice, remove the "writeLine" handler
                deviceClient.SetMethodHandlerAsync("writeLine", null, null).Wait();
                deviceClient.CloseAsync().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        static Task<MethodResponse> WriteLineToConsole(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine();
            Console.WriteLine("\t{0}", methodRequest.DataAsJson);
            Console.WriteLine("\nReturning response for method {0}", methodRequest.Name);

            string result = "'Input was written to log.'";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }
    }
}
