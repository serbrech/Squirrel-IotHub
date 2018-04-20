using Squirrel;
using System;
using System.Configuration;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SquirrelConsole
{
    class Program
    {
        static CancellationToken updateCancellationToken = new CancellationToken();
        //Change the release location to your local Releases folder, or to a url you control!
        static string releaseLocation = ConfigurationManager.AppSettings["releaselocation"];
        static void Main(string[] args)
        {
            //This can be used to inform about update status
            SquirrelAwareApp.HandleEvents(onAppUpdate: v => Console.WriteLine($"updating to {v}"));

            //We run this in the background. do not await. Not using Timer to avoid overlapping runs. I want a sliding interval.
            var notAwaitedTask = RunPeriodicAsync(() => UpdateSimple(releaseLocation), TimeSpan.Zero, TimeSpan.FromSeconds(5), updateCancellationToken);
            if (notAwaitedTask.Status == TaskStatus.Created)
                notAwaitedTask.Start();

            Console.WriteLine("Hello World!");
            Console.WriteLine("Current version is : " + Assembly.GetExecutingAssembly().GetName().Version);
            Thread.Sleep(Timeout.Infinite);
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

        // The `onTick` method will be called periodically unless cancelled.
        private static async Task RunPeriodicAsync(Action onTick,
                                                   TimeSpan dueTime,
                                                   TimeSpan interval,
                                                   CancellationToken token)
        {
            // Initial wait time before we begin the periodic loop.
            if (dueTime > TimeSpan.Zero)
                await Task.Delay(dueTime, token);

            // Repeat this loop until cancelled.
            while (!token.IsCancellationRequested)
            {
                // Call our onTick function.
                onTick?.Invoke();

                // Wait to repeat again.
                if (interval > TimeSpan.Zero)
                    await Task.Delay(interval, token);
            }
        }

    }


}
