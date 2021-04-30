using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlotTracker.Core
{
    public class Exporter
    {

        public void ExportCsv(List<PlotInfo> historicalData, string csvFilename)
        {

            using (StreamWriter sw = new StreamWriter(csvFilename))
            {
                sw.Write("Id");
                sw.Write(",");

                sw.Write("Plot Size");
                sw.Write(",");

                sw.Write("Buffer Size");
                sw.Write(",");

                sw.Write("Buckets");
                sw.Write(",");

                sw.Write("Threads");
                sw.Write(",");

                sw.Write("Stripe Size");
                sw.Write(",");

                sw.Write("Start Date");
                sw.Write(",");

                sw.Write("End Date");
                sw.Write(",");

                sw.Write("Phase 1 Duration");
                sw.Write(",");

                sw.Write("Phase 2 Duration");
                sw.Write(",");

                sw.Write("Phase 3 Duration");
                sw.Write(",");

                sw.Write("Phase 4 Duration");
                sw.Write(",");

                sw.Write("Copy Time");
                sw.Write(",");

                sw.Write("Total Time");
                sw.WriteLine();


                foreach (var plotInfo in historicalData.OrderBy(p => p.StartDate))
                {
                    sw.Write(plotInfo.Id);
                    sw.Write(",");

                    sw.Write(plotInfo.PlotSize);
                    sw.Write(",");

                    sw.Write(plotInfo.BufferSize);
                    sw.Write(",");

                    sw.Write(plotInfo.Buckets);
                    sw.Write(",");

                    sw.Write(plotInfo.Threads);
                    sw.Write(",");

                    sw.Write(plotInfo.StripeSize);
                    sw.Write(",");

                    sw.Write(plotInfo.StartDate);
                    sw.Write(",");

                    sw.Write(plotInfo.EndDate);
                    sw.Write(",");

                    sw.Write(GetCsvString(plotInfo.GetPhaseDuration(1)));
                    sw.Write(",");

                    sw.Write(GetCsvString(plotInfo.GetPhaseDuration(2)));
                    sw.Write(",");

                    sw.Write(GetCsvString(plotInfo.GetPhaseDuration(3)));
                    sw.Write(",");

                    sw.Write(GetCsvString(plotInfo.GetPhaseDuration(4)));
                    sw.Write(",");

                    sw.Write(GetCsvString(plotInfo.CopyTime));
                    sw.Write(",");

                    sw.Write(GetCsvString(plotInfo.TotalTime));
                    sw.WriteLine("");
                }

                sw.Flush();
                sw.Close();
            }
        }

        private string GetCsvString(double? d)
        {
            if (!d.HasValue) return "";
            return d.Value.ToString();
        }
    }
}
