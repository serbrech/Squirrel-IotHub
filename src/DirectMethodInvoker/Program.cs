using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace DirectMethodInvoker
{
    class updateAppPayload
    {
        public string ReleaseLocation { get; set; }
        public string NewVersion { get; set; }
    }

    class Program
    {
        //Change the hubConnectionString to a proper Connection String
        static string hubConnectionString = ConfigurationManager.AppSettings["hubconnectionstring"];
        static ServiceClient serviceClient;

        static void Main(string[] args)
        {
            Console.WriteLine("Connecting to IoT Hub");
            serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);

            Console.WriteLine("Invoking the Direct Method for updating device");
            InvokeMethod().Wait();
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }

        private static async Task InvokeMethod()
        {
            //Properly change ReleaseLocation and NewVersion here
            updateAppPayload payload = new updateAppPayload
            {
                ReleaseLocation = "https://damadmirabe06.blob.core.windows.net/private",
                NewVersion = "4.0.0"
            };

            var methodInvocation = new CloudToDeviceMethod("updateApp") { ResponseTimeout = TimeSpan.FromSeconds(90) };
            methodInvocation.SetPayloadJson(Newtonsoft.Json.JsonConvert.SerializeObject(payload));

            try
            {
                //Properly change the device id here
                var response = await serviceClient.InvokeDeviceMethodAsync("damadmiradevice", methodInvocation);

                Console.WriteLine("Response status: {0}, payload:", response.Status);
                Console.WriteLine(response.GetPayloadAsJson());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
