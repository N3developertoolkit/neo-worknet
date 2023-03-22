
using System.Diagnostics;

namespace NeoShell.Models
{
    class ShellExtension
    {

        public ShellExtension(string name, string subCommand, string mapsToCommand)
        {
            this.Name = name;
            this.SubCommand = subCommand;
            this.MapsToCommand = mapsToCommand;

        }

        public string Name { get; set; }
        public string SubCommand { get; set; }
        public string MapsToCommand { get; set; }

        public int Execute(string[] args, string input, TextWriter sdoutWriter, TextWriter errorWriter)
        {
            var process = new Process();
            process.StartInfo.FileName = this.MapsToCommand;
            var arguments = new List<string>();
            int index = Array.IndexOf(args, this.SubCommand);
            for (int i = index + 1; i < args.Length; i++)
            {
                process.StartInfo.ArgumentList.Add(args[i]);
            }
            if (!string.IsNullOrEmpty(input))
            {
                process.StartInfo.ArgumentList.Add($"--input={input}");
            }
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.OutputDataReceived += (sender, e) => sdoutWriter.WriteLine(e.Data);
            process.Start();
            // Read the output and error streams
            process.BeginOutputReadLine();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(error))
            {
                errorWriter.WriteLine(error);
            }
            return process.ExitCode;
        }
    }
}