using BetterConsoleTables;
using PlotTracker.Core.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace PlotTracker.Core
{
    public class PlotTrackerApp
    {
        private bool _isRunning = true;

        PlotTrackerConfig _config;

        ConcurrentDictionary<Guid, PlotInfo> _plotInfos = new ConcurrentDictionary<Guid, PlotInfo>();
        Dictionary<Guid, Thread> _threads = new Dictionary<Guid, Thread>();
        List<RunningPlot> _runningPlots = new List<RunningPlot>();

        const string TimeSpanFormat = "[-][d':']h':'mm':'ss";

        public PlotTrackerApp(PlotTrackerConfig config)
        {
            _config = config;
        }

        public void Run()
        {
            while (_isRunning)
            {
                var plotInfosClone = _plotInfos.ToArray();

                bool anyInPhase1 = false;

                Table table = new Table("ID", "Start Time", "Current Phase", "Phase Start", "Time In Phase", "Total Time");

                foreach (var plotInfoKvp in plotInfosClone.OrderBy(p=>p.Value.StartDate))
                {
                    var internalId = plotInfoKvp.Key;
                    var plotInfo = plotInfoKvp.Value;

                    string startTime = "";
                    string totalTime = "";
                    if (plotInfo.StartDate.HasValue)
                    {
                        startTime = plotInfo.StartDate.ToString();
                        totalTime = FormatTimespan(DateTime.Now - plotInfo.StartDate.Value);
                    }

                    string currentStatus = "";
                    string phaseStart = "";
                    string timeInPhase = "";

                    var cps = plotInfo.GetCurrentPlotStatus();
                    if (cps != null)
                    {
                        if (cps.PhaseNumber.HasValue && cps.PhaseNumber == 1) anyInPhase1 = true;

                        currentStatus = cps.CurrentPhase;
                        if (cps.StartTime.HasValue)
                        {
                            phaseStart = cps.StartTime.ToString();
                            timeInPhase = FormatTimespan(DateTime.Now - cps.StartTime.Value);
                        }
                    }

                    if (plotInfo.IsComplete)
                    {
                        currentStatus = "Complete";

                        var thisThread = _threads[internalId];
                        thisThread.Join();
                        _threads.Remove(internalId);

                        var thisRunningPlot = _runningPlots.Single(p => p.InternalId == internalId);
                        _runningPlots.Remove(thisRunningPlot);

                        PlotInfo last;
                        _plotInfos.TryRemove(internalId, out last);
                    }

                    table.AddRow(plotInfo.ShortId, startTime, currentStatus, phaseStart, timeInPhase, totalTime);

                    
                }
                Console.Clear();
                if (table.Rows.Count() > 0)
                    Console.WriteLine(table.ToString());
                Console.WriteLine();

                foreach (var tempDirConfig in _config.TempDirs)
                {
                    if (tempDirConfig.ConcurrentPlots == 0) continue;

                    var thesePlots = _runningPlots.Where(p => p.TempDir == tempDirConfig.Path);

                    if (thesePlots.Count() < tempDirConfig.ConcurrentPlots)
                    {
                        if (anyInPhase1)
                        {
                            Console.WriteLine("Waiting to start next plot");
                        }
                        else
                        {
                            Console.WriteLine($"Starting plot creation in temp folder: {tempDirConfig.Path}");
                            StartPlot(tempDirConfig);
                        }
                    }
                }
                
                
                //Console.WriteLine("Sleeping...");
                for (int i = 0; i < 100; i++)
                {
                    Thread.Sleep(100);
                    if (!_isRunning) break;
                }
            }
        }

        private string FormatTimespan(TimeSpan timeSpan)
        {
            string s = timeSpan.ToString("g");
            if (s.Contains("."))
                return s.Substring(0, s.IndexOf("."));
            return s;
        }

        private void StartPlot(TempDirConfig tempDirConfig)
        {
            RunningPlot runningPlot = new RunningPlot()
            {
                InternalId = Guid.NewGuid(),
                TempDir = tempDirConfig.Path
            };

            runningPlot.Temp2Path = tempDirConfig.Temp2Path;
            runningPlot.FinalPath = tempDirConfig.FinalPath;
            runningPlot.LogPath = _config.LogPath;
            runningPlot.PlotSize = tempDirConfig.PlotSize ??_config.DefaultPlotSize ?? null;
            runningPlot.Buffer = tempDirConfig.Buffer ??_config.DefaultBuffer ?? null;
            runningPlot.NumThreads = tempDirConfig.NumThreads ?? _config.DefaultNumThreads ?? null;
            runningPlot.Buckets = tempDirConfig.Buckets ?? _config.DefaultBuckets ?? null;

            

            _runningPlots.Add(runningPlot);

            _plotInfos.TryAdd(runningPlot.InternalId, new PlotInfo());

            Thread t = new Thread(new ParameterizedThreadStart(ExecutePlotThread));
            t.Start(runningPlot);

            _threads.Add(runningPlot.InternalId, t);
        }

        public void Stop()
        {
            _isRunning = false;
        }

        private void ExecutePlotThread(object prms)
        {
            RunningPlot runningPlot = (RunningPlot)prms;

            string cmd = @"C:\code\plottracker\src\PlotTracker\MockPlot\bin\Debug\net5.0\MockPlot.exe";

            StringBuilder sbArgs = new StringBuilder();
            sbArgs.Append("plots create ");

            if (runningPlot.PlotSize.HasValue)
                sbArgs.Append($"-k {runningPlot.PlotSize} ");

            if (runningPlot.Buffer.HasValue)
                sbArgs.Append($"-b {runningPlot.Buffer} ");

            if (runningPlot.Buckets.HasValue)
                sbArgs.Append($"-u {runningPlot.Buckets} ");

            if (runningPlot.NumThreads.HasValue)
                sbArgs.Append($"-u {runningPlot.NumThreads} ");

            sbArgs.Append($"-t \"{runningPlot.TempDir}\" ");
            Directory.CreateDirectory(runningPlot.TempDir);

            if (!string.IsNullOrWhiteSpace(runningPlot.Temp2Path))
            {
                sbArgs.Append($"-2 \"{runningPlot.Temp2Path}\" ");
                Directory.CreateDirectory(runningPlot.Temp2Path);
            }

            sbArgs.Append($"-d \"{runningPlot.FinalPath}\" ");
            Directory.CreateDirectory(runningPlot.FinalPath);

            Directory.CreateDirectory(runningPlot.LogPath);

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = cmd;
            p.StartInfo.Arguments = sbArgs.ToString().Trim();

            p.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.Data)) return;

                PlotInfo plotInfo;
                _plotInfos.TryGetValue(runningPlot.InternalId, out plotInfo);
                
                LogParser.ParseLine(e.Data, ref plotInfo);

                _plotInfos.AddOrUpdate(runningPlot.InternalId, plotInfo, (key, existingVal) =>
                {
                    return plotInfo;
                });

                if (!string.IsNullOrEmpty(plotInfo.Id))
                {
                    string logFilename = Path.Combine(runningPlot.LogPath, $"{plotInfo.Id}.log");
                    using (StreamWriter writer = new StreamWriter(logFilename, true))
                    {
                        writer.WriteLine(e.Data);
                    }
                }
            });

            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();
        }
    }
}
