using System.Diagnostics.CodeAnalysis;
using System.Text;
using Neo;
using Neo.BlockchainToolkit;
using Neo.BlockchainToolkit.Models;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using NeoShell.Models;
using OneOf;
using None = OneOf.Types.None;

namespace NeoShell
{
  static class NodeExtensions
  {
    static bool TryGetContractHash(IReadOnlyList<(UInt160 hash, ContractManifest manifest)> contracts, string name, out UInt160 scriptHash, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
      UInt160? _scriptHash = null;
      for (int i = 0; i < contracts.Count; i++)
      {
        if (contracts[i].manifest.Name.Equals(name, comparison))
        {
          if (_scriptHash is null)
          {
            _scriptHash = contracts[i].hash;
          }
          else
          {
            throw new Exception($"More than one deployed script named {name}");
          }
        }
      }

      if (_scriptHash is not null)
      {
        scriptHash = _scriptHash;
        return true;
      }
      else
      {
        scriptHash = UInt160.Zero;
        return false;
      }
    }

    public static async Task<ContractParameterParser> GetContractParameterParserAsync(this INode expressNode, ExpressChain chain, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
      var contracts = await expressNode.ListContractsAsync().ConfigureAwait(false);
      ContractParameterParser.TryGetUInt160 tryGetContract =
          (string name, [MaybeNullWhen(false)] out UInt160 scriptHash) => TryGetContractHash(contracts, name, out scriptHash, comparison);

      return new ContractParameterParser(expressNode.ProtocolSettings, chain.TryGetAccountHash, tryGetContract);
    }

    public static async Task<UInt256> DeployAsync(this INode expressNode,
                                                          NefFile nefFile,
                                                          ContractManifest manifest,
                                                          Wallet wallet,
                                                          UInt160 accountHash,
                                                          WitnessScope witnessScope,
                                                          ContractParameter? data)
    {
      data ??= new ContractParameter(ContractParameterType.Any);

      // check for bad opcodes (logic borrowed from neo-cli LoadDeploymentScript)
      Neo.VM.Script script = nefFile.Script;
      for (var i = 0; i < script.Length;)
      {
        var instruction = script.GetInstruction(i);
        if (instruction is null)
        {
          throw new FormatException($"null opcode found at {i}");
        }
        else
        {
          if (!Enum.IsDefined(typeof(Neo.VM.OpCode), instruction.OpCode))
          {
            throw new FormatException($"Invalid opcode found at {i}-{((byte)instruction.OpCode).ToString("x2")}");
          }
          i += instruction.Size;
        }
      }

      using var sb = new ScriptBuilder();
      sb.EmitDynamicCall(NativeContract.ContractManagement.Hash,
          "deploy",
          nefFile.ToArray(),
          manifest.ToJson().ToString(),
          data);
      return await expressNode.ExecuteAsync(wallet, accountHash, witnessScope, sb.ToArray()).ConfigureAwait(false);
    }

    public static async Task<UInt256> UpdateAsync(this INode expressNode,
                                                          NefFile nefFile,
                                                          ContractManifest manifest,
                                                          Wallet wallet,
                                                          UInt160 accountHash,
                                                          WitnessScope witnessScope,
                                                          ContractParameter? data)
    {
      data ??= new ContractParameter(ContractParameterType.Any);

      // check for bad opcodes (logic borrowed from neo-cli LoadDeploymentScript)
      Neo.VM.Script script = nefFile.Script;
      for (var i = 0; i < script.Length;)
      {
        var instruction = script.GetInstruction(i);
        if (instruction is null)
        {
          throw new FormatException($"null opcode found at {i}");
        }
        else
        {
          if (!Enum.IsDefined(typeof(Neo.VM.OpCode), instruction.OpCode))
          {
            throw new FormatException($"Invalid opcode found at {i}-{((byte)instruction.OpCode).ToString("x2")}");
          }
          i += instruction.Size;
        }
      }

      using var sb = new ScriptBuilder();
      sb.EmitDynamicCall(NativeContract.ContractManagement.Hash,
          "update",
          nefFile.ToArray(),
          manifest.ToJson().ToString(),
          data);
      return await expressNode.ExecuteAsync(wallet, accountHash, witnessScope, sb.ToArray()).ConfigureAwait(false);
    }

    public static async Task<OneOf<UInt160, None>> TryGetAccountHashAsync(this INode expressNode, ExpressChain chain, string name)
    {
      if (name.StartsWith('#'))
      {
        var contracts = await expressNode.ListContractsAsync().ConfigureAwait(false);
        if (TryGetContractHash(contracts, name.Substring(1), out var contractHash))
        {
          return contractHash;
        }
      }

      if (chain.TryGetAccountHash(name, out var accountHash))
      {
        return accountHash;
      }

      return default(None);
    }

    public static async Task<UInt160> ParseAssetAsync(this INode expressNode, string asset)
    {
      if ("neo".Equals(asset, StringComparison.OrdinalIgnoreCase))
      {
        return NativeContract.NEO.Hash;
      }

      if ("gas".Equals(asset, StringComparison.OrdinalIgnoreCase))
      {
        return NativeContract.GAS.Hash;
      }

      if (UInt160.TryParse(asset, out var uint160))
      {
        return uint160;
      }

      var contracts = await expressNode.ListTokenContractsAsync().ConfigureAwait(false);
      for (int i = 0; i < contracts.Count; i++)
      {
        if (contracts[i].Standard == TokenStandard.Nep17
            && contracts[i].Symbol.Equals(asset, StringComparison.OrdinalIgnoreCase))
        {
          return contracts[i].ScriptHash;
        }
      }

      throw new ArgumentException($"Unknown Asset \"{asset}\"", nameof(asset));
    }

    public static async Task<(RpcNep17Balance balance, Nep17Contract token)> GetBalanceAsync(this INode expressNode, UInt160 accountHash, string asset)
    {
      var assetHash = await expressNode.ParseAssetAsync(asset).ConfigureAwait(false);

      using var sb = new ScriptBuilder();
      sb.EmitDynamicCall(assetHash, "balanceOf", accountHash);
      sb.EmitDynamicCall(assetHash, "symbol");
      sb.EmitDynamicCall(assetHash, "decimals");

      var result = await expressNode.InvokeAsync(sb.ToArray()).ConfigureAwait(false);
      var stack = result.Stack;
      if (stack.Length >= 3)
      {
        var balance = stack[0].GetInteger();
        var symbol = Encoding.UTF8.GetString(stack[1].GetSpan());
        var decimals = (byte)(stack[2].GetInteger());

        return (
            new RpcNep17Balance() { Amount = balance, AssetHash = assetHash },
            new Nep17Contract(symbol, decimals, assetHash));
      }

      throw new Exception("invalid script results");
    }

    public static async Task<Block> GetBlockAsync(this INode expressNode, string blockHash)
    {
      if (string.IsNullOrEmpty(blockHash))
      {
        return await expressNode.GetLatestBlockAsync().ConfigureAwait(false);
      }

      if (UInt256.TryParse(blockHash, out var uint256))
      {
        return await expressNode.GetBlockAsync(uint256).ConfigureAwait(false);
      }

      if (uint.TryParse(blockHash, out var index))
      {
        return await expressNode.GetBlockAsync(index).ConfigureAwait(false);
      }

      throw new ArgumentException($"{nameof(blockHash)} must be block index, block hash or empty", nameof(blockHash));
    }

    internal static async Task<PolicyValues> GetPolicyAsync(Func<Script, Task<RpcInvokeResult>> invokeAsync)
    {
      using var builder = new ScriptBuilder();
      builder.EmitDynamicCall(NativeContract.NEO.Hash, "getGasPerBlock");
      builder.EmitDynamicCall(NativeContract.ContractManagement.Hash, "getMinimumDeploymentFee");
      builder.EmitDynamicCall(NativeContract.NEO.Hash, "getRegisterPrice");
      builder.EmitDynamicCall(NativeContract.Oracle.Hash, "getPrice");
      builder.EmitDynamicCall(NativeContract.Policy.Hash, "getFeePerByte");
      builder.EmitDynamicCall(NativeContract.Policy.Hash, "getStoragePrice");
      builder.EmitDynamicCall(NativeContract.Policy.Hash, "getExecFeeFactor");

      var result = await invokeAsync(builder.ToArray()).ConfigureAwait(false);

      if (result.State != VMState.HALT) throw new Exception(result.Exception);
      if (result.Stack.Length != 7) throw new InvalidOperationException();

      return new PolicyValues()
      {
        GasPerBlock = new BigDecimal(result.Stack[0].GetInteger(), NativeContract.GAS.Decimals),
        MinimumDeploymentFee = new BigDecimal(result.Stack[1].GetInteger(), NativeContract.GAS.Decimals),
        CandidateRegistrationFee = new BigDecimal(result.Stack[2].GetInteger(), NativeContract.GAS.Decimals),
        OracleRequestFee = new BigDecimal(result.Stack[3].GetInteger(), NativeContract.GAS.Decimals),
        NetworkFeePerByte = new BigDecimal(result.Stack[4].GetInteger(), NativeContract.GAS.Decimals),
        StorageFeeFactor = (uint)result.Stack[5].GetInteger(),
        ExecutionFeeFactor = (uint)result.Stack[6].GetInteger(),
      };
    }
  }
}