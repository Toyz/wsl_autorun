using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace wsl_service_autorun
{
    class Program
    {
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        static void Main(string[] args)
        {
            ShowWindow(GetConsoleWindow(), SW_HIDE);

            var bashCmd = RunBash("-c", "'cat /etc/os-release'");
            var releaseInfo = ReadReleaseInfo(bashCmd);

            var autorunFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                ".wsl_autostart");
            var filePath = Path.Combine(autorunFolder, "autorun.txt");

            if (!Directory.Exists(autorunFolder))
            {
                Directory.CreateDirectory(autorunFolder);
                File.WriteAllText(filePath, "");
                MessageBox.Show($"To have this program auto start programs edit\n\n{filePath}", "First run", MessageBoxButtons.OK);
                return;
            }

            if (!File.Exists(filePath))
            {
                MessageBox.Show($"To have this program auto start programs edit\n\n{filePath}", "First run", MessageBoxButtons.OK);
                return;
            }

            var services = File.ReadAllLines(filePath);
            if (services.Length <= 0)
            {
                MessageBox.Show($"To have this program auto start programs edit\n\n{filePath}", "First run", MessageBoxButtons.OK);
                return;
            }


            Console.WriteLine($"Running: {releaseInfo["pretty_name"]}");
            foreach (var service in services)
            {
                var sr = RunService(service);
                foreach (var s in sr)
                {
                    Console.WriteLine($"{service}: {s}");
                }
            }
        }

        static Dictionary<string, string> ReadReleaseInfo(List<string> file)
        {
            var output = new Dictionary<string, string>();

            foreach (var line in file)
            {
                var values = line.Split(new []{ '=' }, 2);
                output.Add(values[0].ToLower(), values[1]);
            }

            return output;
        }

        static List<string> RunService(string service)
        {
            return RunBash("-c", $"'sudo service {service} restart'");
        }

        static List<string> RunBash(params string[] args)
        {
            var enviromentPath = Environment.GetFolderPath(Environment.SpecialFolder.System);

            var output = new List<string>();
            var StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(enviromentPath, "bash.exe"),
                Arguments = string.Join(" ", args),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            StartInfo.EnvironmentVariables["PATH"] = enviromentPath;


            var proc = new Process {StartInfo = StartInfo};

            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                output.Add(line.Replace("\"", ""));
            }

            return output;
        }
    }
}
