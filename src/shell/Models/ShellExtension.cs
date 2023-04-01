
using System.Diagnostics;
using Neo;
using Neo.Network.P2P.Payloads;
using Neo.Wallets;
using Newtonsoft.Json;

namespace NeoShell.Models
{
    class ShellExtension
    {

        public record SubCommand(string Command, bool isTransaction);
        public record InvocationParameter(string Contract, string Method, string Account, string[] Arguments);

        public ShellExtension()
        {

        }

        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string MapsToCommand { get; set; } = string.Empty;
        public SubCommand[] Commands { get; init; } = Array.Empty<SubCommand>();

        public bool IsTransaction(string subCommand)
        {
            var commandInfo = this.Commands.FirstOrDefault(c => String.Equals(c.Command, subCommand, StringComparison.OrdinalIgnoreCase));
            return commandInfo?.isTransaction ?? false;
        }
        // public int Execute(string[] args, string input, TextWriter sdoutWriter, TextWriter errorWriter)
        // {
        //     var process = new Process();
        //     process.StartInfo.FileName = this.MapsToCommand;
        //     var arguments = new List<string>();
        //     int index = Array.IndexOf(args, this.Command);
        //     for (int i = index + 1; i < args.Length; i++)
        //     {
        //         process.StartInfo.ArgumentList.Add(args[i]);
        //     }
        //     if (!string.IsNullOrEmpty(input))
        //     {
        //         process.StartInfo.ArgumentList.Add($"--input={input}");
        //     }
        //     process.StartInfo.UseShellExecute = false;
        //     process.StartInfo.RedirectStandardOutput = true;
        //     process.StartInfo.RedirectStandardError = true;
        //     process.OutputDataReceived += (sender, e) => sdoutWriter.WriteLine(e.Data);
        //     process.Start();
        //     // Read the output and error streams
        //     process.BeginOutputReadLine();
        //     var error = process.StandardError.ReadToEnd();

        //     process.WaitForExit();

        //     if (!string.IsNullOrWhiteSpace(error))
        //     {
        //         errorWriter.WriteLine(error);
        //     }
        //     return process.ExitCode;
        // }

        public async Task<int> ExecuteAsync(string[] args, string input, TextWriter sdoutWriter, TextWriter errorWriter, ExpressChainManagerFactory? chainManagerFactory, TransactionExecutorFactory? txExecutorFactory)
        {
            var process = new Process();
            process.StartInfo.FileName = this.MapsToCommand;
            var arguments = new List<string>();
            int index = Array.IndexOf(args, this.Command);
            string subCommand = args[index + 1];
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
            process.Start();
            process.WaitForExit();
            // Read the output and error streams
            using var reader = new StreamReader(process.StandardOutput.BaseStream);
            string output = await reader.ReadToEndAsync();
            if (this.IsTransaction(subCommand))
            {
                var invokeParams = JsonConvert.DeserializeObject<InvocationParameter>(output);
                if (invokeParams == null)
                {
                    throw new Exception("Invalid invocation parameters from the subcommand.");
                }
                if (chainManagerFactory == null || txExecutorFactory == null)
                {
                    throw new Exception("ChainManagerFactory or TransactionExecutorFactory is not initialized.");
                }

                var (chainManager, _) = chainManagerFactory.LoadChain(input);
                using var txExec = txExecutorFactory.Create(chainManager, true, false); //TODO: need to handle Trace and JSON
                var script = await txExec.BuildInvocationScriptAsync(invokeParams.Contract, invokeParams.Method, invokeParams.Arguments).ConfigureAwait(false);
                await txExec.InvokeForResultsAsync(script, invokeParams.Account, WitnessScope.CalledByEntry).ConfigureAwait(false);
            }
            else
            {
                await sdoutWriter.WriteLineAsync(output);
            }

            var error = await process.StandardError.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(error))
            {
                await errorWriter.WriteLineAsync(error);
            }
            return process.ExitCode;
        }
    }
}