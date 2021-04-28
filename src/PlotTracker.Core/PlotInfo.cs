using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlotTracker.Core
{
    public class CurrentPlotStatus
    {
        public string CurrentPhase { get; set; }
        public DateTime? StartTime { get; set; }
        public int? PhaseNumber{ get; set; }
    }

    public class PlotInfo
    {
        public PlotInfo()
        {
            Phases = new List<PhaseInfo>();
        }

        public CurrentPlotStatus GetCurrentPlotStatus()
        {
            if (this.IsComplete) return null;

            CurrentPlotStatus cps = new CurrentPlotStatus();

            if (Phases.Count == 0)
            {
                cps.CurrentPhase = "Starting";
                return cps;
            }

            var currentPhase = Phases.OrderByDescending(p => p.Number).FirstOrDefault();

            cps.CurrentPhase = $"Phase {currentPhase.Number}";
            cps.StartTime = currentPhase.StartDate;
            cps.PhaseNumber = currentPhase.Number;

            if (!string.IsNullOrWhiteSpace(FinalFileSize))
            {
                cps.CurrentPhase = "Copying";
                cps.StartTime = currentPhase.EndDate;
                cps.PhaseNumber = null;
            }

            return cps;
        }

        public string ShortId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Id)) return "0000000";
                return Id.Substring(Id.Length - 7);
            }
        }

        public string Id { get; set; }
        public int? PlotSize { get; set; }
        public string BufferSize { get; set; }
        public int? Buckets{ get; set; }
        public int? Threads{ get; set; }
        public int? StripeSize{ get; set; }

        public List<PhaseInfo> Phases{ get; set; }

        public string ApproxWorkingSpace{ get; set; }
        public string FinalFileSize{ get; set; }
        public double? TotalTime { get; set; }
        public string CPU{ get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string CopyFileSource{ get; set; }
        public string CopyFileDest{ get; set; }
        public double? CopyTime{ get; set; }
        public string CopyCPU{ get; set; }

        public bool IsComplete{ get; set; }

        public string RenameSource { get; set; }
        public string RenameDest { get; set; }

        public string TempPath { get; set; }
        public string Temp2Path { get; set; }
    }

    public class PhaseInfo
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Number { get; set; }

        public double Duration{ get; set; }
        public string CPU{ get; set; }
    }
}
