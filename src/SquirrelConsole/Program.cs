using Squirrel;
using System;
using System.Configuration;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using System.Text;
using Newtonsoft.Json;

namespace SquirrelConsole
{
    class updateAppPayload
    {
        public string ReleaseLocation { get; set; }
        public string NewVersion { get; set; }
    }
    class Program
    {
        //Change the deviceConnectionString to a proper Connection String
        static string deviceConnectionString = ConfigurationManager.AppSettings["deviceconnectionstring"];
        static DeviceClient Client = null;


        static void Main(string[] args)
        {
            try
            {
                //This can be used to inform about update status
                SquirrelAwareApp.HandleEvents(onAppUpdate: v => Console.WriteLine($"updating to {v}"));

                Console.WriteLine("Current version is : " + Assembly.GetExecutingAssembly().GetName().Version);

                Console.WriteLine("Connecting to IoT Hub");
                Client = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);

                // Set callback for "updateApp" method
                Client.SetMethodHandlerAsync("updateApp", updateAppMethod, null).Wait();
                Console.WriteLine("Waiting for direct method call\n Press enter to exit.");
                Console.ReadLine();

                Console.WriteLine("Exiting...");

                // Remove the "updateApp" handler
                Client.SetMethodHandlerAsync("updateApp", null, null).Wait();
                Client.CloseAsync().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }


        static async Task<MethodResponse> updateAppMethod(MethodRequest methodRequest, object userContext)
        {
            updateAppPayload payload;
            string result;
            try
            {
                payload = JsonConvert.DeserializeObject<updateAppPayload>(methodRequest.DataAsJson);

                result = "{\"Status\":\"Started update to new version}\"}";
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    UpdateSimple(payload.ReleaseLocation);
                }).Start();
                return new MethodResponse(Encoding.UTF8.GetBytes(result), 200);
            }
            catch (Exception ex)
            {
                return new MethodResponse(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ex.Message)), 500);
            }
        }


        private static void UpdateSimple(string releaseLocation)
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                ReleaseEntry releaseEntry;
                using (var mgr = new UpdateManager(releaseLocation))
                {
                    Console.WriteLine($"{DateTime.Now.ToLongTimeString()} - Checking for updates at {releaseLocation}");

                    releaseEntry = mgr.UpdateApp(progress =>
                    {
                        if (progress % 10 == 0) Console.WriteLine($"{progress}%");
                    }).GetAwaiter().GetResult();
                }

                if (releaseEntry != null)
                {
                    UpdateManager.RestartApp();
                }
            }
        }

        private static async Task UpdateWithFullControl(string releaseLocation)
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                bool newVersionInstalled = false;
                using (var mgr = new UpdateManager(releaseLocation))
                {
                    Console.WriteLine($"{DateTime.Now.ToLongTimeString()} - Checking for updates at {releaseLocation}");
                    var updateInfo = await mgr.CheckForUpdate();
                    if (updateInfo.FutureReleaseEntry.Version > mgr.CurrentlyInstalledVersion())
                    {
                        Console.WriteLine("We have updates to apply!");
                        Console.WriteLine("Downloading updates...");
                        await mgr.DownloadReleases(updateInfo.ReleasesToApply, progress => { if (progress % 10 == 0) Console.WriteLine($"{progress}%"); });
                        Console.WriteLine("Applying updates...");
                        var releasePath = await mgr.ApplyReleases(updateInfo);
                        Console.WriteLine($"release ready in {releasePath}");
                        newVersionInstalled = true;
                    }
                }

                if (newVersionInstalled)
                {
                    UpdateManager.RestartApp();
                }
            }
        }
    }
}
