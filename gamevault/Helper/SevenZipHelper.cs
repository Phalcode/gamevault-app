using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;


namespace gamevault.Helper
{
    public class SevenZipProgressEventArgs : EventArgs
    {
        public int PercentageDone { get; set; }
        public SevenZipProgressEventArgs(int percentageDone)
        {
            PercentageDone = percentageDone;
        }
    }
    internal static class ProcessShepherd
    {
        private static List<Process> childProcesses = new List<Process>();
        internal static void AddProcess(Process process)
        {
            childProcesses.Add(process);
        }
        internal static void RemoveProcess(Process process)
        {
            childProcesses.Remove(process);
        }
        internal static void KillAllChildProcesses()
        {
            for (int count = 0; count < childProcesses.Count; count++)
            {
                try
                {
                    if (!childProcesses[count].HasExited)
                    {
                        childProcesses[count].Kill();
                    }
                }
                catch { }
            }
        }

    }
    internal class SevenZipHelper
    {
        private Process process { get; set; }
        public delegate void ProcessHandler(object sender, SevenZipProgressEventArgs e);
        public event ProcessHandler Process;
        private ProcessStartInfo CreateProcessHeader()
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;
            info.FileName = $"{AppDomain.CurrentDomain.BaseDirectory}Lib\\7z\\7z.exe";
            return info;
        }

        internal async Task<int> ExtractArchive(string archivePath, string outputDir)
        {
            await Task.Run(() =>
             {
                 process = new Process();
                 ProcessShepherd.AddProcess(process);
                 process.StartInfo = CreateProcessHeader();
                 process.StartInfo.Arguments = $"x -y -bsp1 -o\"{outputDir}\" \"{archivePath}\"";
                 process.EnableRaisingEvents = true;
                 process.OutputDataReceived += (sender, e) =>
                 {
                     if (Process != null)
                     {
                         if (e.Data != null && e.Data.Contains("%"))
                         {
                             int index = e.Data.IndexOf("%");
                             string percentageStr = e.Data.Substring(0, index).Replace(" ", "");
                             if (int.TryParse(percentageStr, out int percentage))
                             {
                                 Process(this, new SevenZipProgressEventArgs(percentage));
                             }
                         }
                     }
                 };

                 if (!Directory.Exists(outputDir))
                 {
                     Directory.CreateDirectory(outputDir);
                 }

                 process.Start();
                 process.BeginOutputReadLine();
                 process.WaitForExit();
                 ProcessShepherd.RemoveProcess(process);
             });
            try
            {
                return process.ExitCode;
            }
            catch
            {
                return -1;
            }
        }
        internal void Cancel()
        {
            if (process != null)
            {
                process.Kill();
            }
        }
    }
}
