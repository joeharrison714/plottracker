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
        private const string StartingPlotToken = "Starting plotting progress into temporary dirs";

        public static List<PlotInfo> ParseFile(string filename)
        {
            List<PlotInfo> plotInfos = new List<PlotInfo>();
            PlotInfo currentPlotInfo = null;

            using (Stream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {

                using (StreamReader rdr = new StreamReader(stream))
                {
                    string line;
                    while ((line = rdr.ReadLine()) != null)
                    {
                        if (line.StartsWith(StartingPlotToken))
                        {
                            currentPlotInfo = new PlotInfo();
                            plotInfos.Add(currentPlotInfo);
                        }

                        if (currentPlotInfo == null) continue;
                        ParseLine(line, ref currentPlotInfo);
                    }
                }
            }

            return plotInfos;
        }

        private const string StartingToken = "Starting phase ";
        private const string EndingToken = "Time for phase ";
        private const string DateSep = "... ";
        private const string TotalTimeToken = "Total time ";
        private const string CopyToken = "Copied final file from ";
        private const string CopyEndingToken = "Copy time ";
        private const string RenameToken = "Renamed final file from ";

        public static void ParseLine(string line, ref PlotInfo plotInfo)
        {
            if (line.StartsWith(StartingPlotToken))
            {
                var startLine = line.Substring(StartingPlotToken.Length + 1).Trim();
                var parts = startLine.Split(" and ");

                plotInfo.TempPath = parts[0];
                plotInfo.Temp2Path = parts[1];
            }
            else if (line.StartsWith("ID: "))
            {
                plotInfo.Id = TakeRight(":", line);
            }
            else if (line.StartsWith("Plot size is:"))
            {
                plotInfo.PlotSize = int.Parse(TakeRight(":", line));
            }
            else if (line.StartsWith("Buffer size is:"))
            {
                plotInfo.BufferSize = TakeRight(":", line);
            }
            else if (line.StartsWith("Using "))
            {
                var usingParts = line.Split(" ");
                if (usingParts.Length == 7 && usingParts[2] == "threads")
                {
                    plotInfo.Threads = int.Parse(usingParts[1]);
                    plotInfo.StripeSize = int.Parse(usingParts[6]);
                }
                else if (usingParts.Length == 3 && usingParts[2] == "buckets")
                {
                    plotInfo.Buckets = int.Parse(usingParts[1]);
                }
            }
            else if (line.StartsWith(StartingToken))
            {
                string tmp = line.Substring(StartingToken.Length);

                string phase = tmp.Substring(0, tmp.IndexOf(":"));
                string date = tmp.Substring(tmp.IndexOf(DateSep) + DateSep.Length);

                var pi = new PhaseInfo()
                {
                    Number = ParsePhaseNumber(phase),
                    StartDate = ParseDate(date),
                    Duration = null
                };

                plotInfo.Phases.Add(pi);

                if (pi.Number == 1)
                    plotInfo.StartDate = pi.StartDate;
            }
            else if (line.StartsWith(EndingToken))
            {
                string tmp = line.Substring(EndingToken.Length);

                int phase = int.Parse(tmp.Substring(0, tmp.IndexOf("=")));

                var thisPhase = plotInfo.Phases.Single(p => p.Number == phase);

                var tpl = ParseEndLine(EndingToken, line);

                thisPhase.Duration = tpl.Item1;
                thisPhase.CPU = tpl.Item2;
                thisPhase.EndDate = tpl.Item3;
            }
            else if (line.StartsWith("Approximate working space used"))
            {
                plotInfo.ApproxWorkingSpace = TakeRight(":", line);
            }
            else if (line.StartsWith("Final File size:"))
            {
                plotInfo.FinalFileSize = TakeRight(":", line);
            }
            else if (line.StartsWith(TotalTimeToken))
            {
                var tpl = ParseEndLine(TotalTimeToken, line);

                plotInfo.TotalTime = tpl.Item1;
                plotInfo.CPU = tpl.Item2;
                plotInfo.EndDate = tpl.Item3;
            }
            else if (line.StartsWith(CopyToken))
            {
                string tmp = line.Substring(CopyToken.Length);
                var parts = tmp.Split(" to ");
                plotInfo.CopyFileSource = Between(parts[0], "\"", "\"");
                plotInfo.CopyFileDest = Between(parts[1], "\"", "\"");
            }
            else if (line.StartsWith(CopyEndingToken))
            {
                var tpl = ParseEndLine(CopyEndingToken, line);

                plotInfo.CopyTime = tpl.Item1;
                plotInfo.CopyCPU = tpl.Item2;
                plotInfo.EndDate = tpl.Item3;
            }
            else if (line.StartsWith(RenameToken))
            {
                string tmp = line.Substring(RenameToken.Length);
                var parts = tmp.Split(" to ");
                plotInfo.RenameSource = Between(parts[0], "\"", "\"");
                plotInfo.RenameDest = Between(parts[1], "\"", "\"");

                plotInfo.IsComplete = true;
            }
        }

        private static Tuple<double, string, DateTime> ParseEndLine(string prefix, string line)
        {
            string tmp = line.Substring(prefix.Length);

            string seconds = Between(tmp, "= ", " seconds.").Trim();

            string cpu = Between(tmp, "CPU (", ")").Trim();

            string date = tmp.Substring(tmp.IndexOf(")") + 1).Trim();

            return new Tuple<double, string, DateTime>(double.Parse(seconds), cpu, ParseDate(date));
        }

        private static string Between(string str, string firstString, string lastString)
        {
            int pos1 = str.IndexOf(firstString) + firstString.Length;
            int pos2 = str.Substring(pos1).IndexOf(lastString);
            return str.Substring(pos1, pos2);
        }

        private static DateTime ParseDate(string str){
            //Fri Apr 23 20:45:41 2021
            var parts = str.Split(" ");
            return DateTime.Parse($"{parts[1]} {parts[2]} {parts[4]} {parts[3]}");
        }

        private static int ParsePhaseNumber(string str)
        {
            var parts = str.Split('/');
            return int.Parse(parts[0]);
        }

        private static string TakeRight(string sep, string line)
        {
            var parts = line.Split(sep);
            return parts[1].Trim();
        }
    }
}
