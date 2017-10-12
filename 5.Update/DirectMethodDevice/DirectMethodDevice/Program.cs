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
                deviceClient.SetMethodHandlerAsync("firmwareUpdate", FirmwareUpdate, null);

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

            // remove the 'WriteToConsole' handler
            deviceClient?.SetMethodHandlerAsync("firmwareUpdate", null, null);

            // remove the 'GetDeviceName' handler
            // Method Call processing will be disabled when the last method handler has been removed .
            //deviceClient?.SetMethodHandler("GetDeviceName", null, null);
        }


        public static string Uri;
        public static Task<MethodResponse> FirmwareUpdate(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine();
            Console.WriteLine("\t{0}", methodRequest.DataAsJson);
            Console.WriteLine();

            
            var payload = JsonConvert.DeserializeObject<dynamic>(methodRequest.DataAsJson);
            var uri = (string)payload.fwPackageUri;

            if (string.IsNullOrWhiteSpace(uri))
            {
                throw new ArgumentException("Missing FwPackageUri");
            }

            Uri = uri;

            ReportConnectivity(Uri, "waiting", "startedWaitingTime", null);            

            ReportConnectivity(Uri, "downloading", "downloadCompleteTime", null);            

            ReportConnectivity(Uri, "applying", "startedApplyingImage", null);

            ReportConnectivity(Uri, "applyComplete", "lastFirmwareUpdate", null);

            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject("succeed")), 200));
        }


        public static async void ReportConnectivity(string uri, string status, string updatetname, string error)
        {
            await deviceClient.GetTwinAsync();

            //var patch = new
            //{
            //    properties = new
            //    {
            //        reported = new
            //        {
            //            iothubdm = new
            //            {
            //                firmwareUpdate = new
            //                {
            //                    fwPackageUri = uri, 
            //                    status = status,
            //                    updatetname = DateTime.UtcNow.ToString()        }
            //            }
            //        }
            //    }
            //};

            TwinCollection reportedProperties, iothubDM, firmwareUpdate;

            reportedProperties = new TwinCollection();
            iothubDM = new TwinCollection();
            firmwareUpdate = new TwinCollection();

            firmwareUpdate[updatetname] = DateTime.UtcNow.ToString();
            firmwareUpdate["fwPackageUri"] = uri;
            firmwareUpdate["status"] = status;
            firmwareUpdate["error"] = error;

            iothubDM["firmwareUpdate"] = firmwareUpdate;
            reportedProperties["iothubDM"] = iothubDM;

            Console.WriteLine("\t twin state reported: {0}", status);

            await Task.Delay(2000);

            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
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
