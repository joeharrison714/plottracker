using BetterConsoleTables;
using Newtonsoft.Json;
using PlotTracker.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlotTracker.Core
{
    public class PlotTrackerMonitorApp
    {
        const int HistoryDays = 7;
        const int DeleteLogsAfterDays = 2;
        private readonly PlotTrackerConfig _config;
        //string LogPath = @"C:\chia\logs";
        //string ParsedLogPath = @"C:\chia\logs\parsed";

        private readonly PlotInfoRepository _plotInfoRepository;

        private bool _isRunning = true;

        public PlotTrackerMonitorApp(PlotTrackerConfig config)
        {
            _config = config;

            _plotInfoRepository = new PlotInfoRepository(_config.ParsedLogPath);
        }
        public void RunMonitor()
        {
            while (_isRunning)
            {
                Console.Clear();

                List<PlotInfo> allPlotInfos = LoadAndParseLogs();

                WriteCurrentStatus(allPlotInfos);

                List<PlotInfo> historicalData = _plotInfoRepository.GetAll();

                WriteHistorySummary(historicalData);

                bool tookAction = CheckShouldStart(allPlotInfos);
                if (tookAction)
                {
                    continue; // a plot was started, so re-evaluate before sleeping
                }

                PromptAndSleep();
            }
        }

        public void RunCsvExport()
        {
            string csvFilename = Path.Combine(_config.ParsedLogPath, $"data-{DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss")}.csv");

            List<PlotInfo> historicalData = _plotInfoRepository.GetAll();

            Exporter e = new Exporter();

            e.ExportCsv(historicalData, csvFilename);
        }



        private void PromptAndSleep()
        {
            Console.WriteLine("Will autorefresh. Or press (r) to refresh now or (x) to exit");

            bool _keepAsking = true;

            DateTime beginWait = DateTime.Now;
            while (_keepAsking && DateTime.Now.Subtract(beginWait).TotalSeconds < 60)
            {
                if (Console.KeyAvailable)
                {
                    switch (Console.ReadKey(true).KeyChar)
                    {
                        case 'x':
                        case 'X':
                            _isRunning = false;
                            _keepAsking = false;
                            break;
                        //case 'm':
                        //case 'M':
                        //    Menu();
                        //    break;
                        case 'r':
                        case 'R':
                            _keepAsking = false;
                            break;
                        default:
                            Console.WriteLine("Invalid!");
                            break;
                    }

                }
                Thread.Sleep(250);
            }
        }


        private bool CheckShouldStart(List<PlotInfo> allPlotInfos)
        {
            var runningPlots = allPlotInfos.Where(p => !p.IsComplete).ToList();

            bool tookAction = false;

            string finalPath = GetFinalDir();
            Console.WriteLine($"Next final dir to be used: {finalPath}");

            var tempDirs = GetRunningPlotsByTempDir(runningPlots);

            foreach (var tempDir in tempDirs.OrderBy(p=>p.RunningPlotCount).ThenBy(p=>p.MostRecentPlotStartDate))
            {                
                Console.WriteLine($"{tempDir.TempPath} has {tempDir.RunningPlotCount} running plots (Most recent started at {tempDir.MostRecentPlotStartDate})");

                if (tempDir.RunningPlotCount < tempDir.TempDirConfig.ConcurrentPlots)
                {
                    bool shouldStart = false;

                    PlotInfo mostRecent = null;

                    if (_config.StaggerDelayIsGlobal)
                    {
                        mostRecent = runningPlots.Where(p => p.StartDate.HasValue).OrderByDescending(p => p.StartDate).FirstOrDefault();
                    }
                    else
                    {
                        mostRecent = tempDir.RunningPlots.Where(p => p.StartDate.HasValue).OrderByDescending(p => p.StartDate).FirstOrDefault();
                    }

                    if (mostRecent == null)
                    {
                        shouldStart = true;
                    }
                    else
                    {
                        var mostRecentTs = DateTime.Now - mostRecent.StartDate.Value;

                        if (mostRecentTs.TotalSeconds > tempDir.TempDirConfig.StaggerDelaySeconds)
                        {
                            shouldStart = true;
                        }
                        else
                        {
                            var when = new TimeSpan(0, 0, tempDir.TempDirConfig.StaggerDelaySeconds - (int)Math.Round(mostRecentTs.TotalSeconds));
                            Console.WriteLine($"Will start new plot in {FormatTimespan(when)}");
                        }
                    }

                    



                    if (shouldStart)
                    {
                        if (finalPath == null)
                        {
                            Console.WriteLine("No available final dirs with enough space!");
                        }
                        else
                        {
                            Console.WriteLine("Using final path: " + finalPath);

                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.FileName = tempDir.TempDirConfig.StartCommand;
                            startInfo.Arguments = tempDir.TempDirConfig.StartArgs;
                            startInfo.UseShellExecute = false;
                            startInfo.EnvironmentVariables.Add("TempPath", tempDir.TempDirConfig.Path);
                            startInfo.EnvironmentVariables.Add("LogPath", _config.LogPath);
                            startInfo.EnvironmentVariables.Add("FinalPath", finalPath);
                            Process process = new Process();
                            process.StartInfo = startInfo;
                            process.Start();
                            tookAction = true;
                            Thread.Sleep(10000);
                        }
                    }

                    //if (anyInPhase1)
                    //{
                    //    Console.WriteLine("Waiting to start next plot");
                    //}
                    //else
                    //{
                    //    Console.WriteLine($"Starting plot creation in temp folder: {tempDirConfig.Path}");
                    //    StartPlot(tempDirConfig);
                    //}

                    if (tookAction) break; // exit loop so currently running plots will be re-evaluated
                }
                else
                    Console.WriteLine("Concurrent plots met");
            }
            return tookAction;
        }

        private List<TempDirRunningPlot> GetRunningPlotsByTempDir(List<PlotInfo> runningPlots)
        {
            List<TempDirRunningPlot> tempDirRunningPlots = new List<TempDirRunningPlot>();

            foreach (var tempDirConfig in _config.TempDirs)
            {
                if (tempDirConfig.ConcurrentPlots == 0) continue;

                TempDirRunningPlot tempDir = new TempDirRunningPlot();
                tempDir.TempDirConfig = tempDirConfig;
                tempDir.TempPath = tempDirConfig.Path;

                tempDir.RunningPlots = runningPlots.Where(p => PathEquals(p.TempPath, tempDirConfig.Path)).ToList();

                tempDir.RunningPlotCount = tempDir.RunningPlots.Count;

                if (tempDir.RunningPlotCount > 0)
                {
                    var f = tempDir.RunningPlots.OrderByDescending(p => p.StartDate).FirstOrDefault();
                    if (f != null)
                    {
                        tempDir.MostRecentPlotStartDate = f.StartDate;
                    }
                }

                tempDirRunningPlots.Add(tempDir);
            }

            return tempDirRunningPlots;
        }

        const long NeededSpace = 108905839000;
        private string GetFinalDir()
        {
            Dictionary<string, long> useablePaths = new Dictionary<string, long>();

            foreach (var fd in _config.FinalDirs)
            {
                DriveInfo di = new DriveInfo(Path.GetPathRoot(fd.Path));
                //Console.WriteLine($"{di.RootDirectory} - {di.AvailableFreeSpace}");
                if (di.AvailableFreeSpace > NeededSpace)
                {
                    useablePaths.Add(fd.Path, di.AvailableFreeSpace);
                }
            }

            if (!useablePaths.Any()) return null;

            if (_config.FinalDirMostFirst)
                return useablePaths.OrderByDescending(p => p.Value).First().Key;
            return useablePaths.OrderBy(p => p.Value).First().Key;
        }

        internal bool PathEquals(string path1, string path2)
        {
            return Path.GetFullPath(path1)
                .Equals(Path.GetFullPath(path2), StringComparison.InvariantCultureIgnoreCase);
        }

        private List<PlotInfo> LoadAndParseLogs()
        {
            Directory.CreateDirectory(_config.ParsedLogPath);

            DirectoryInfo logsDir = new DirectoryInfo(_config.LogPath);

            List<PlotInfo> allPlotInfos = new List<PlotInfo>();

            HashSet<string> toDelete = new HashSet<string>();

            foreach (var logFile in logsDir.GetFiles("*.txt"))
            {
                //Console.WriteLine($"Parsing {logFile}");

                var plots = LogParser.ParseFile(logFile.FullName);

                var numComplete = plots.Count(p => p.IsComplete);
                var numNotComplete = plots.Count(p => !p.IsComplete);

                //Console.WriteLine($"Got {plots.Count()} plots. Completed: {numComplete} Incomplete: {numNotComplete}");

                allPlotInfos.AddRange(plots);

                foreach (var plot in plots)
                {
                    if (plot.IsComplete && !_plotInfoRepository.Exists(plot.Id))
                    {
                        //Console.WriteLine($"Saving {plot.Id}");
                        _plotInfoRepository.Save(plot);
                    }
                }

                if (plots.Count > 0 && numNotComplete == 0)
                {
                    var mostRecentFinish = plots.Where(p => p.EndDate.HasValue).Max(p => p.EndDate.Value);
                    var ts = DateTime.Now - mostRecentFinish;
                    if (ts.TotalDays > DeleteLogsAfterDays)
                    {
                        toDelete.Add(logFile.FullName);
                    }
                }
            }

            foreach (var del in toDelete)
            {
                Console.WriteLine($"Deleting {del}");
                File.Delete(del);
            }

            return allPlotInfos;
        }

        private void WriteHistorySummary(List<PlotInfo> historicalData)
        {
            Dictionary<DateTime, List<PlotInfo>> plotsByDay = new Dictionary<DateTime, List<PlotInfo>>();


            foreach (var plot in historicalData)
            {
                if (!plot.IsComplete) continue;
                if (!plot.EndDate.HasValue) continue;

                DateTime justDay = plot.EndDate.Value.Date;
                if (!plotsByDay.ContainsKey(justDay))
                    plotsByDay.Add(justDay, new List<PlotInfo>());
                plotsByDay[justDay].Add(plot);
            }

            Table table = new Table("Date", "Plots", "Avg Phase 1", "Avg Phase 2", "Avg Phase 3", "Avg Phase 4", "Avg. Copy", "Avg. Total");

            foreach (var pbd in plotsByDay.OrderByDescending(p => p.Key).Take(HistoryDays))
            {
                var plots = pbd.Value;

                var avgTotalSeconds = (int)Math.Ceiling(plots.Where(p => p.TotalTime.HasValue).Average(p => p.TotalTime.Value));
                TimeSpan avgTotal = new TimeSpan(0, 0, avgTotalSeconds);

                var avgPhase1Seconds = (int)Math.Ceiling(plots.SelectMany(p => p.Phases.Where(q => q.Number == 1).Select(q => q.Duration.Value)).Average());
                TimeSpan avgPhase1 = new TimeSpan(0, 0, avgPhase1Seconds);

                var avgPhase2Seconds = (int)Math.Ceiling(plots.SelectMany(p => p.Phases.Where(q => q.Number == 2).Select(q => q.Duration.Value)).Average());
                TimeSpan avgPhase2 = new TimeSpan(0, 0, avgPhase2Seconds);

                var avgPhase3Seconds = (int)Math.Ceiling(plots.SelectMany(p => p.Phases.Where(q => q.Number == 3).Select(q => q.Duration.Value)).Average());
                TimeSpan avgPhase3 = new TimeSpan(0, 0, avgPhase3Seconds);

                var avgPhase4Seconds = (int)Math.Ceiling(plots.SelectMany(p => p.Phases.Where(q => q.Number == 4).Select(q => q.Duration.Value)).Average());
                TimeSpan avgPhase4 = new TimeSpan(0, 0, avgPhase4Seconds);

                var avgCopySeconds = (int)Math.Ceiling(plots.Where(p => p.CopyTime.HasValue).Average(p => p.CopyTime.Value));
                TimeSpan avgCopy = new TimeSpan(0, 0, avgCopySeconds);

                table.AddRow(pbd.Key.ToShortDateString(), plots.Count(), FormatTimespan(avgPhase1), FormatTimespan(avgPhase2),
                    FormatTimespan(avgPhase3), FormatTimespan(avgPhase4), FormatTimespan(avgCopy), FormatTimespan(avgTotal));
            }

            if (table.Rows.Count() > 0)
                Console.WriteLine(table.ToString());
        }



        private void WriteCurrentStatus(List<PlotInfo> allPlotInfos)
        {
            Table table = new Table("ID", "Start Time", "Temp Drive", "Current Phase", "Phase Start", "Time In Phase", "%", "Total Time");

            foreach (var plotInfo in allPlotInfos.Where(p => !p.IsComplete).OrderBy(p => p.StartDate))
            {

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
                string pct = "";

                var cps = plotInfo.GetCurrentPlotStatus();
                if (cps != null)
                {
                    //if (cps.PhaseNumber.HasValue && cps.PhaseNumber == 1) anyInPhase1 = true;

                    currentStatus = cps.CurrentPhase;
                    if (cps.StartTime.HasValue)
                    {
                        phaseStart = cps.StartTime.ToString();
                        timeInPhase = FormatTimespan(DateTime.Now - cps.StartTime.Value);
                    }
                    pct = cps.PercentComplete.ToString("P0");
                }

                if (plotInfo.IsComplete)
                {
                    currentStatus = "Complete";
                }

                string tempDrive = Directory.GetDirectoryRoot(plotInfo.TempPath);

                table.AddRow(plotInfo.ShortId, startTime, tempDrive, currentStatus, phaseStart, timeInPhase, pct, totalTime);


            }
            if (table.Rows.Count() > 0)
                Console.WriteLine(table.ToString());
        }

        private string FormatTimespan(TimeSpan timeSpan)
        {
            string s = timeSpan.ToString("g");
            if (s.Contains("."))
                return s.Substring(0, s.IndexOf("."));
            return s;
        }
    }
}
