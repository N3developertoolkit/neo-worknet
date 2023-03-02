using Neo.BlockchainToolkit;
using Neo.BlockchainToolkit.Models;
using Newtonsoft.Json;

namespace NeoWorkNet;

class JsonWalletWriter : IWalletWriter
{
    readonly JsonTextWriter writer;

    public JsonWalletWriter(JsonTextWriter writer)
    {
        this.writer = writer;
        writer.WriteStartArray();
    }
    public JsonWalletWriter(TextWriter writer)
        : this(new JsonTextWriter(writer) { Formatting = Formatting.Indented })
    {
    }

    public void Dispose()
    {
        writer.WriteEndArray();
    }

    public void WriteWallet(ToolkitWallet wallet)
    {
        writer.WriteStartObject();
        writer.WriteProperty("name", wallet.Name);

        writer.WritePropertyName("accounts");
        writer.WriteStartArray();
        foreach (var account in wallet.GetAccounts())
        {
            var keyPair = account.GetKey() ?? throw new Exception();

            writer.WriteStartObject();
            writer.WritePropertyName("account-label");
            writer.WriteValue(account.IsDefault ? "Default" : account.Label);
            writer.WritePropertyName("address");
            writer.WriteValue(account.Address);
            writer.WritePropertyName("script-hash");
            writer.WriteValue(account.ScriptHash.ToString());
            writer.WritePropertyName("private-key");
            writer.WriteValue(Convert.ToHexString(keyPair.PrivateKey));
            writer.WritePropertyName("private-key-wif");
            writer.WriteValue(keyPair.Export());
            writer.WritePropertyName("public-key");
            writer.WriteValue(Convert.ToHexString(keyPair.PublicKey.EncodePoint(true)));
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
