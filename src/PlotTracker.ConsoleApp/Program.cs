using Microsoft.Extensions.Configuration;
using PlotTracker.Core;
using PlotTracker.Core.Configuration;
using System;

namespace PlotTracker.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //var builder = new ConfigurationBuilder()
            //    .AddJsonFile($"appsettings.json", true, true);

            //var config = builder.Build();

            //var ptc = new PlotTrackerConfig();
            //config.GetSection("plottracker").Bind(ptc);

            //PlotTrackerApp app = new PlotTrackerApp(ptc);
            //app.Run();


            PlotTrackerMonitorApp monitorApp = new PlotTrackerMonitorApp();
            monitorApp.Run();

            //LogParser.ParseFile(@"C:\code\plottracker\src\PlotTracker\MockPlot\stdout.txt");
        }
    }
}
