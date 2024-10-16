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
            info.RedirectStandardError = true;
            info.UseShellExecute = false;
            info.FileName = $"{AppDomain.CurrentDomain.BaseDirectory}Lib\\7z\\7z.exe";
            return info;
        }
        internal async Task<bool> IsArchiveEncrypted(string archivePath)
        {
            bool result = false;
            Process process = new Process();
            ProcessShepherd.AddProcess(process);
            process.StartInfo = CreateProcessHeader();
            process.StartInfo.Arguments = $"l -slt -pskibidibopmmdadap \"{archivePath}\"";
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (error.Contains("encrypted", StringComparison.OrdinalIgnoreCase) || output.Contains("Encrypted = +"))
            {
                result = true;
            }
            ProcessShepherd.RemoveProcess(process);
            return result;
        }
        internal async Task<int> ExtractArchive(string archivePath, string outputDir, string password = "")
        {
            int exitCode = -1;
            await Task.Run(() =>
             {
                 process = new Process();
                 ProcessShepherd.AddProcess(process);
                 process.StartInfo = CreateProcessHeader();
                 process.StartInfo.Arguments = $"x -y -bsp1 -o\"{outputDir}\" \"{archivePath}\"";
                 if (password != "")
                 {
                     process.StartInfo.Arguments += $" -p{password}";
                 }
                 process.EnableRaisingEvents = true;
                 process.ErrorDataReceived += (sender, e) =>
                 {
                     if (e.Data != null && e.Data.Contains("Wrong password"))
                     {
                         exitCode = 69;
                     }
                 };
                 process.OutputDataReceived += (sender, e) =>
                 {
                     if (Process == null)
                     {
                         return;
                     }
                     if (e.Data != null && e.Data.Contains("%"))
                     {
                         int index = e.Data.IndexOf("%");
                         string percentageStr = e.Data.Substring(0, index).Replace(" ", "");
                         if (int.TryParse(percentageStr, out int percentage))
                         {
                             Process(this, new SevenZipProgressEventArgs(percentage));
                         }
                     }
                 };

                 if (!Directory.Exists(outputDir))
                 {
                     Directory.CreateDirectory(outputDir);
                 }

                 process.Start();
                 process.BeginOutputReadLine();
                 process.BeginErrorReadLine();
                 process.WaitForExit();
                 ProcessShepherd.RemoveProcess(process);
             });
            try
            {
                if (exitCode == -1)
                {
                    return process.ExitCode;
                }
                return exitCode;
            }
            catch
            {
                return exitCode;
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
