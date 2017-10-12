using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
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

        class DeviceData
        {
            public DeviceData(string myName)
            {
                this.Name = myName;
            }

            public string Name
            {
                get; set;
            }
        }

        

        static void Main(string[] args)
        {
            try
            {
                deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Mqtt);

                deviceClient.OpenAsync().Wait();

                // Method Call processing will be enabled when the first method handler is added.
                // setup a callback for the 'WriteToConsole' method

                deviceClient.GetTwinAsync(); 
                deviceClient.SetMethodHandlerAsync("reboot", OnReboot, null);

                //ReportConnectivity(); 


                // setup a calback for the 'GetDeviceName' method
                //deviceClient.SetMethodHandler("GetDeviceName", GetDeviceName, new DeviceData("DeviceClientMethodMqttSample"));
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error in sample: {0}", exception);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
            Console.WriteLine("Press enter to exit...");

            Console.ReadLine();
            Console.WriteLine("Exiting...");

            // remove the 'reboot' handler
            //deviceClient?.SetMethodHandlerAsync("WriteToConsole", null, null);
            deviceClient.SetMethodHandlerAsync("reboot", null, null).Wait();
            deviceClient.CloseAsync().Wait();

            // remove the 'GetDeviceName' handler
            // Method Call processing will be disabled when the last method handler has been removed .
            //deviceClient?.SetMethodHandler("GetDeviceName", null, null);
        }

        public  static Task<MethodResponse> OnReboot(MethodRequest methodRequest, object userContext)
        {           
            TwinCollection reportedProperties, iothubDM, reboot;

            reportedProperties = new TwinCollection();
            iothubDM = new TwinCollection();
            reboot = new TwinCollection();

            reboot["lastReboot"] = DateTime.UtcNow.ToString();
            iothubDM["reboot"] = reboot;
            reportedProperties["iothubDM"] = iothubDM;

            deviceClient.UpdateReportedPropertiesAsync(reportedProperties);

            
            string result = "'Reboot started.'";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));

        }

        public static async void ReportConnectivity()
        {
            try
            {
                Console.WriteLine("Sending connectivity data as reported property");

                await deviceClient.GetTwinAsync(); 

                TwinCollection reportedProperties, iothubDM, reboot;

                reportedProperties = new TwinCollection();
                iothubDM = new TwinCollection(); 
                reboot = new TwinCollection();

                reboot["lastReboot"] = DateTime.UtcNow.ToString();
                iothubDM["reboot"] = reboot;
                reportedProperties["iothubDM"] = iothubDM; 

                await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        private async static Task RebootAsync()
        {
            await SetReportedPropertyAsync("Method.Reboot", null);
        }

        protected async static Task SetReportedPropertyAsync(string name, dynamic value)
        {
            var collection = new TwinCollection();
            collection[name] = value; 

            //TwinCollectionExtension.Set(collection, name, value);
            await deviceClient.UpdateReportedPropertiesAsync(collection);
        }

        public static Task<MethodResponse> GetDeviceName(MethodRequest methodRequest, object userContext)
        {
            MethodResponse retValue;
            if (userContext == null)
            {
                retValue = new MethodResponse(new byte[0], 500);
            }
            else
            {
                var d = userContext as DeviceData;
                string result = "{\"name\":\"" + d.Name + "\"}";
                retValue = new MethodResponse(Encoding.UTF8.GetBytes(result), 200);
            }
            return Task.FromResult(retValue);
        }
    }
}
