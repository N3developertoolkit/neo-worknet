using System.IO.Abstractions;
using System.Numerics;
using Neo;
using Neo.BlockchainToolkit;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;
using Newtonsoft.Json;

namespace NeoShell
{
  using All = OneOf.Types.All;

  class TransactionExecutor : IDisposable
  {
    readonly ChainManager chainManager;
    readonly INode expressNode;
    readonly IFileSystem fileSystem;
    readonly bool json;
    readonly System.IO.TextWriter writer;

    public TransactionExecutor(IFileSystem fileSystem, ChainManager chainManager, bool trace, bool json, TextWriter writer)
    {
      this.chainManager = chainManager;
      expressNode = chainManager.GetNode(trace);
      this.fileSystem = fileSystem;
      this.json = json;
      this.writer = writer;
    }

    public void Dispose()
    {
      expressNode.Dispose();
    }

    public INode ExpressNode => expressNode;

    public async Task<Script> LoadInvocationScriptAsync(string invocationFile)
    {
      if (!fileSystem.File.Exists(invocationFile))
      {
        throw new Exception($"Invocation file {invocationFile} couldn't be found");
      }

      var parser = await expressNode.GetContractParameterParserAsync(chainManager.Chain).ConfigureAwait(false);
      return await parser.LoadInvocationScriptAsync(invocationFile).ConfigureAwait(false);
    }

    public async Task<Script> BuildInvocationScriptAsync(string contract, string operation, IReadOnlyList<string>? arguments = null)
    {
      if (string.IsNullOrEmpty(operation))
        throw new InvalidOperationException($"invalid contract operation \"{operation}\"");

      var parser = await expressNode.GetContractParameterParserAsync(chainManager.Chain).ConfigureAwait(false);
      var scriptHash = parser.TryLoadScriptHash(contract, out var value)
          ? value
          : UInt160.TryParse(contract, out var uint160)
              ? uint160
              : throw new InvalidOperationException($"contract \"{contract}\" not found");

      arguments ??= Array.Empty<string>();
      var @params = new ContractParameter[arguments.Count];
      for (int i = 0; i < arguments.Count; i++)
      {
        @params[i] = ConvertArg(arguments[i], parser);
      }

      using var scriptBuilder = new ScriptBuilder();
      scriptBuilder.EmitDynamicCall(scriptHash, operation, @params);
      return scriptBuilder.ToArray();

      static ContractParameter ConvertArg(string arg, ContractParameterParser parser)
      {
        if (bool.TryParse(arg, out var boolArg))
        {
          return new ContractParameter()
          {
            Type = ContractParameterType.Boolean,
            Value = boolArg
          };
        }

        if (long.TryParse(arg, out var longArg))
        {
          return new ContractParameter()
          {
            Type = ContractParameterType.Integer,
            Value = new BigInteger(longArg)
          };
        }

        return parser.ParseParameter(arg);
      }
    }

    public async Task ContractInvokeAsync(Script script, string accountName, string password, WitnessScope witnessScope, decimal additionalGas = 0m)
    {
      if (!chainManager.TryGetSigningAccount(accountName, password, out var wallet, out var accountHash))
      {
        throw new Exception($"{accountName} account not found.");
      }

      var txHash = await expressNode.ExecuteAsync(wallet, accountHash, witnessScope, script, additionalGas).ConfigureAwait(false);
      await writer.WriteTxHashAsync(txHash, "Invocation", json).ConfigureAwait(false);
    }

    public async Task InvokeForResultsAsync(Script script, string accountName, WitnessScope witnessScope)
    {
      Signer? signer = chainManager.TryGetSigningAccount(accountName, string.Empty, out _, out var accountHash)
          ? signer = new Signer
          {
            Account = accountHash,
            Scopes = witnessScope,
            AllowedContracts = Array.Empty<UInt160>(),
            AllowedGroups = Array.Empty<Neo.Cryptography.ECC.ECPoint>()
          }
          : null;

      var result = await expressNode.InvokeAsync(script, signer).ConfigureAwait(false);
      if (json)
      {
        await writer.WriteLineAsync(result.ToJson().ToString(true)).ConfigureAwait(false);
      }
      else
      {
        await writer.WriteLineAsync($"VM State:     {result.State}").ConfigureAwait(false);
        await writer.WriteLineAsync($"Gas Consumed: {result.GasConsumed}").ConfigureAwait(false);
        if (!string.IsNullOrEmpty(result.Exception))
        {
          await writer.WriteLineAsync($"Exception:   {result.Exception}").ConfigureAwait(false);
        }
        if (result.Stack.Length > 0)
        {
          var stack = result.Stack;
          await writer.WriteLineAsync("Result Stack:").ConfigureAwait(false);
          for (int i = 0; i < stack.Length; i++)
          {
            await WriteStackItemAsync(writer, stack[i]).ConfigureAwait(false);
          }
        }
      }
    }

    public async Task ContractDeployAsync(string contract, string accountName, string password, WitnessScope witnessScope, string data, bool force)
    {
      if (!chainManager.TryGetSigningAccount(accountName, password, out var wallet, out var accountHash))
      {
        throw new Exception($"{accountName} account not found.");
      }

      var (nefFile, manifest) = await fileSystem.LoadContractAsync(contract).ConfigureAwait(false);

      ContractParameter dataParam;
      if (string.IsNullOrEmpty(data))
      {
        dataParam = new ContractParameter(ContractParameterType.Any);
      }
      else
      {
        var parser = await expressNode.GetContractParameterParserAsync(chainManager.Chain).ConfigureAwait(false);
        dataParam = parser.ParseParameter(data);
      }

      var txHash = await expressNode
          .DeployAsync(nefFile, manifest, wallet, accountHash, witnessScope, dataParam)
          .ConfigureAwait(false);

      var contractHash = Neo.SmartContract.Helper.GetContractHash(accountHash, nefFile.CheckSum, manifest.Name);
      if (json)
      {
        using var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented };
        await jsonWriter.WriteStartObjectAsync().ConfigureAwait(false);
        await jsonWriter.WritePropertyNameAsync("contract-name").ConfigureAwait(false);
        await jsonWriter.WriteValueAsync(manifest.Name).ConfigureAwait(false);
        await jsonWriter.WritePropertyNameAsync("contract-hash").ConfigureAwait(false);
        await jsonWriter.WriteValueAsync($"{contractHash}").ConfigureAwait(false);
        await jsonWriter.WritePropertyNameAsync("tx-hash").ConfigureAwait(false);
        await jsonWriter.WriteValueAsync($"{txHash}").ConfigureAwait(false);
        await jsonWriter.WriteEndObjectAsync().ConfigureAwait(false);
      }
      else
      {
        await writer.WriteLineAsync($"Deployment of {manifest.Name} ({contractHash}) Transaction {txHash} submitted").ConfigureAwait(false);
      }
    }

    public async Task ContractUpdateAsync(string contract, string nef, string accountName, string password, WitnessScope witnessScope, string data, bool force)
    {
      if (!chainManager.TryGetSigningAccount(accountName, password, out var wallet, out var accountHash))
      {
        throw new Exception($"{accountName} account not found.");
      }
      var parser = await expressNode.GetContractParameterParserAsync(chainManager.Chain).ConfigureAwait(false);
      var scriptHash = parser.TryLoadScriptHash(contract, out var value)
          ? value
          : UInt160.TryParse(contract, out var uint160)
              ? uint160
              : throw new InvalidOperationException($"contract \"{contract}\" not found");

      var originalManifest = await expressNode.GetContractAsync(scriptHash).ConfigureAwait(false);
      var updateMethod = originalManifest.Abi.GetMethod("update", 2);
      if (updateMethod == null)
      {
        throw new Exception($"update method on {contract} contract not found.");
      }
      if (updateMethod.Parameters[0].Type != ContractParameterType.ByteArray
             || updateMethod.Parameters[1].Type != ContractParameterType.String)
      {
        throw new Exception($"update method on {contract} contract has unexpected signature.");
      }

      var (nefFile, manifest) = await fileSystem.LoadContractAsync(nef).ConfigureAwait(false);
      var txHash = await expressNode
                      .UpdateAsync(scriptHash, nefFile, manifest, wallet, accountHash, witnessScope)
                      .ConfigureAwait(false);
      await writer.WriteTxHashAsync(txHash, "Update", json).ConfigureAwait(false);
    }

    static async Task WriteStackItemAsync(System.IO.TextWriter writer, Neo.VM.Types.StackItem item, int indent = 1, string prefix = "")
    {
      switch (item)
      {
        case Neo.VM.Types.Boolean _:
          await WriteLineAsync(item.GetBoolean() ? "true" : "false").ConfigureAwait(false);
          break;
        case Neo.VM.Types.Integer @int:
          await WriteLineAsync(@int.GetInteger().ToString()).ConfigureAwait(false);
          break;
        case Neo.VM.Types.Buffer buffer:
          await WriteLineAsync(Neo.Helper.ToHexString(buffer.GetSpan())).ConfigureAwait(false);
          break;
        case Neo.VM.Types.ByteString byteString:
          await WriteLineAsync(Neo.Helper.ToHexString(byteString.GetSpan())).ConfigureAwait(false);
          break;
        case Neo.VM.Types.Null _:
          await WriteLineAsync("<null>").ConfigureAwait(false);
          break;
        case Neo.VM.Types.Array array:
          await WriteLineAsync($"Array: ({array.Count})").ConfigureAwait(false);
          for (int i = 0; i < array.Count; i++)
          {
            await WriteStackItemAsync(writer, array[i], indent + 1).ConfigureAwait(false);
          }
          break;
        case Neo.VM.Types.Map map:
          await WriteLineAsync($"Map: ({map.Count})").ConfigureAwait(false);
          foreach (var m in map)
          {
            await WriteStackItemAsync(writer, m.Key, indent + 1, "key:   ").ConfigureAwait(false);
            await WriteStackItemAsync(writer, m.Value, indent + 1, "value: ").ConfigureAwait(false);
          }
          break;
      }

      async Task WriteLineAsync(string value)
      {
        for (var i = 0; i < indent; i++)
        {
          await writer.WriteAsync("  ").ConfigureAwait(false);
        }

        if (!string.IsNullOrEmpty(prefix))
        {
          await writer.WriteAsync(prefix).ConfigureAwait(false);
        }

        await writer.WriteLineAsync(value).ConfigureAwait(false);
      }
    }
  }
}
