using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlotTracker.Core
{
    internal class RunningPlot
    {
        internal Guid InternalId { get; set; }
        internal string TempDir { get; set; }


        public string Temp2Path { get; set; }
        public string FinalPath { get; set; }

        public string LogPath { get; set; }
        public int? PlotSize { get; set; }
        public int? Buffer { get; set; }
        public int? NumThreads { get; set; }
        public int? Buckets { get; set; }
    }
}
