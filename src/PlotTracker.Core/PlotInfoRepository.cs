using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlotTracker.Core
{
    public class PlotInfoRepository
    {
        private readonly string _parsedDir;

        private Dictionary<string, PlotInfo> _plotInfoCache = new Dictionary<string, PlotInfo>();

        public PlotInfoRepository(string parsedDir)
        {
            _parsedDir = parsedDir;
        }

        public bool Exists(string id)
        {
            string filename = GetFilename(id);
            return File.Exists(filename);
        }

        public void Save(PlotInfo plotInfo)
        {
            string filename = GetFilename(plotInfo.Id);

            
            string json = JsonConvert.SerializeObject(plotInfo, Formatting.Indented);
            using (StreamWriter writer = new StreamWriter(filename))
            {
                writer.Write(json);
                writer.Flush();
                writer.Close();
            }

        }

        private string GetFilename(string id)
        {
            return Path.Combine(_parsedDir, id + ".json");
        }
        public List<PlotInfo> GetAll()
        {
            DirectoryInfo logsDir = new DirectoryInfo(_parsedDir);

            List<PlotInfo> historicalData = new List<PlotInfo>();

            foreach (var plotFile in logsDir.GetFiles("*.json"))
            {
                string fn = Path.GetFileNameWithoutExtension(plotFile.Name);

                PlotInfo plotInfo;
                if (_plotInfoCache.ContainsKey(fn))
                {
                    plotInfo = _plotInfoCache[fn];
                }
                else
                {
                    var json = File.ReadAllText(plotFile.FullName);
                    plotInfo = JsonConvert.DeserializeObject<PlotInfo>(json);
                    _plotInfoCache.Add(fn, plotInfo);
                }

                historicalData.Add(plotInfo);
            }
            return historicalData;
        }
    }
}
