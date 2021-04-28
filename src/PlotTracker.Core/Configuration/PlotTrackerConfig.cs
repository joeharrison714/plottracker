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

        public int? DefaultPlotSize{ get; set; }
        public int? DefaultBuffer{ get; set; }
        public int? DefaultNumThreads{ get; set; }
        public int? DefaultBuckets{ get; set; }
    }

    public class TempDirConfig
    {
        public string Path{ get; set; }
        public string Temp2Path { get; set; }
        public string FinalPath{ get; set; }
        public int ConcurrentPlots{ get; set; }

        public int? PlotSize{ get; set; }
        public int? Buffer{ get; set; }
        public int? NumThreads{ get; set; }
        public int? Buckets{ get; set; }
    }
}
