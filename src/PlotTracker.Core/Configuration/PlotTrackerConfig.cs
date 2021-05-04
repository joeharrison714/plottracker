using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlotTracker.Core.Configuration
{
    public class PlotTrackerConfig
    {
        public List<TempDirConfig> TempDirs{ get; set; }
        public string LogPath{ get; set; }
        public string ParsedLogPath { get; set; }
        public bool StaggerDelayIsGlobal{ get; set; }

        public List<FinalDirConfig> FinalDirs { get; set; }
        public bool FinalDirMostFirst { get; set; }

        public List<MaintenanceWindowConfigEntry> MaintenanceWindows{ get; set; }
    }

    public class FinalDirConfig
    {
        public string Path { get; set; }
    }

    public class TempDirConfig
    {
        public string Path{ get; set; }
        public int ConcurrentPlots { get; set; }
        public int StaggerDelaySeconds { get; set; }
        public string StartCommand{ get; set; }
        public string StartArgs{ get; set; }
    }

    public class MaintenanceWindowConfigEntry
    {
        public DayOfWeek Weekday{ get; set; }
        public string StartTime{ get; set; }
        public string Duration { get; set; }
    }
}
