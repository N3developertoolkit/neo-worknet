
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
        public record InvocationParameter(string Script, string Account, bool Trace, bool Json);

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
            var (subCommandName, index) = ExtractCommandFromArguments(args, this.Command);
            if(string.IsNullOrWhiteSpace(subCommandName) || index < 0)
            {
                throw new Exception("Invalid subcommand.");
            }
            Process process = CreateNewProcess(args, input, index);
            process.Start();
            process.WaitForExit();
            using var reader = new StreamReader(process.StandardOutput.BaseStream);
            string output = await reader.ReadToEndAsync();
            var subCommand = this.Find(subCommandName);
            if (subCommand != null && subCommand.InvokeContract)
            {
                await HandleTransactionsAsync(input, chainManagerFactory, txExecutorFactory, output, subCommand).ConfigureAwait(false);
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

        private static async Task<TransactionExecutor> HandleTransactionsAsync(string input, ExpressChainManagerFactory? chainManagerFactory, TransactionExecutorFactory? txExecutorFactory, string output, SubCommand subCommand)
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
            var txExec = txExecutorFactory.Create(chainManager, invokeParams.Trace, invokeParams.Json);
            var script = new Script(Convert.FromBase64String(invokeParams.Script));
            if (subCommand.Safe)
            {
                await txExec.InvokeForResultsAsync(script, invokeParams.Account ?? string.Empty, WitnessScope.CalledByEntry).ConfigureAwait(false);
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

            return txExec;
        }

        private Process CreateNewProcess(string[] args, string input, int index)
        {
            var process = new Process();
            process.StartInfo.FileName = this.MapsToCommand;
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
            return process;
        }

        private (string, int) ExtractCommandFromArguments(string[] args, string command)
        {
            var index = Array.IndexOf(args, command);
            var subCommandName = args[index + 1];
            return (subCommandName, index);
        }

    }
}