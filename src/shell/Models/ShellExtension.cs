
using System.Diagnostics;

namespace NeoShell.Models
{
    class ShellExtension
    {

        public ShellExtension(string name, string command)
        {
            this.Name = name;
            this.Command = command;

        }

        public string Name { get; set; }
        public string Command { get; set; }

        public int Execute(string[] args, string input, TextWriter sdoutWriter, TextWriter errorWriter)
        {
            var process = new Process();
            process.StartInfo.FileName = this.Command;
            var arguments = new List<string>();
            int index = Array.IndexOf(args, this.Command);
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