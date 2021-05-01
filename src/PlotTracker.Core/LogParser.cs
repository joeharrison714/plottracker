using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlotTracker.Core
{
    public static class LogParser
    {
        internal const string StartingPlotToken = "Starting plotting progress into temporary dirs";

        public static List<PlotInfo> ParseFile(string filename)
        {
            List<PlotInfo> plotInfos = new List<PlotInfo>();
            PlotInfoParser currentPlotInfoParser = null;

            using (Stream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {

                using (StreamReader rdr = new StreamReader(stream))
                {
                    string line;
                    while ((line = rdr.ReadLine()) != null)
                    {
                        if (line.StartsWith(StartingPlotToken))
                        {
                            currentPlotInfoParser = new PlotInfoParser();
                            plotInfos.Add(currentPlotInfoParser.PlotInfo);
                        }

                        if (currentPlotInfoParser == null) continue;
                        currentPlotInfoParser.ParseLine(line);
                    }
                }
            }

            return plotInfos;
        }

    }
}
