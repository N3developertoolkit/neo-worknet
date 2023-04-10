
using System.Diagnostics;
using Neo;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using Newtonsoft.Json;

namespace NeoShell.Models
{
    class ShellExtension
    {

        public record SubCommand(string Command, bool InvokeContract, bool Safe);
        public record InvocationParameter(string Script, string Account);

        public ShellExtension()
        {

        }

        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string MapsToCommand { get; set; } = string.Empty;
        public SubCommand[] Commands { get; init; } = Array.Empty<SubCommand>();

        public SubCommand? Find(string subCommand)
        {
            var commandInfo = this.Commands.FirstOrDefault(c => String.Equals(c.Command, subCommand, StringComparison.OrdinalIgnoreCase));
            return commandInfo;
        }

        public async Task<int> ExecuteAsync(string[] args, string input, TextWriter sdoutWriter, TextWriter errorWriter, ExpressChainManagerFactory? chainManagerFactory, TransactionExecutorFactory? txExecutorFactory)
        {
            var process = new Process();
            process.StartInfo.FileName = this.MapsToCommand;
            var arguments = new List<string>();
            int index = Array.IndexOf(args, this.Command);
            string subCommandName = args[index + 1];
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
            var subCommand = this.Find(subCommandName);
            if (subCommand != null && subCommand.InvokeContract)
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
                var script = new Script(Convert.FromBase64String(invokeParams.Script));
                if (subCommand.Safe)
                {
                    await txExec.InvokeForResultsAsync(script, "node1", WitnessScope.CalledByEntry).ConfigureAwait(false);
                }
                else
                {
                    var password = string.Empty;
                    if (!invokeParams.Account.IsWIFString())
                    {
                        password = chainManager.Chain.ResolvePassword(invokeParams.Account, password);
                        await txExec.ContractInvokeAsync(script, invokeParams.Account, password, WitnessScope.CalledByEntry, 0);
                    }
                }
                // 

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