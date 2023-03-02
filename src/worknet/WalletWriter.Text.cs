using Neo.BlockchainToolkit.Models;

namespace NeoWorkNet;

class TextWalletWriter : IWalletWriter
{
    readonly TextWriter writer;

    public TextWalletWriter(TextWriter writer)
    {
        this.writer = writer;
    }

    public void Dispose() { }

    public void WriteWallet(ToolkitWallet wallet)
    {
        writer.WriteLine(wallet.Name);
        foreach (var account in wallet.GetAccounts())
        {
            var keyPair = account.GetKey() ?? throw new Exception();

            writer.WriteLine($"  {account.Address} ({(account.IsDefault ? "Default" : account.Label)})");
            writer.WriteLine($"    script hash:       {account.ScriptHash}");
            writer.WriteLine($"    public key:        {Convert.ToHexString(keyPair.PublicKey.EncodePoint(true))}");
            writer.WriteLine($"    private key (hex): {Convert.ToHexString(keyPair.PrivateKey)}");
            writer.WriteLine($"    private key (WIF): {keyPair.Export()}");
        }
    }
}
