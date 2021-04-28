using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace MockPlot
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Args: " + string.Join(' ', args));
            Console.WriteLine();

            string id = Guid.NewGuid().ToString().Replace("-", "");

            using (StreamReader rdr = new StreamReader(Path.Combine(AssemblyDirectory, "stdout.txt")))
            {
                string line;
                while ((line = rdr.ReadLine()) != null)
                {
                    line = line.Replace("{{id}}", id);
                    System.Console.WriteLine(line);
                    Thread.Sleep(50);
                }
            }
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().Location;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
