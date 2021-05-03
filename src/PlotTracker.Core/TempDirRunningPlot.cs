using PlotTracker.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlotTracker.Core
{
    internal class TempDirRunningPlot
    {
        public TempDirRunningPlot()
        {
            RunningPlots = new List<PlotInfo>();
        }
        public string TempPath{ get; set; }
        public TempDirConfig TempDirConfig{ get; set; }
        public List<PlotInfo> RunningPlots{ get; set; }
        public int RunningPlotCount{ get; set; }
        public DateTime? MostRecentPlotStartDate{ get; set; }
    }
}
