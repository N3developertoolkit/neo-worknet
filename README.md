# Neo Shell and Worknet (Preview)

Neo Shell is a cross-platform, extensible, unified command-line interface for managing Neo N3 chain resources on worknet, testnet etc. The Neo Shell enables developers to execute commands through a terminal using interactive command-line 
prompts or scripts. Similar in spirit, and inspired by the likes of Unix-style shells, "neosh" is an extensible command processor that runs in a terminal window. 

With Neo Shell, developers can perform various tasks such as deploying contracts, invoking contracts, querying blocks, 
transactions, addresses and more. Neo Shell support for custom commands and extensions is in the works. Neo Shell is designed with extensibility and customization in mind, and we expect that the Neo N3 ecosystem and communities will extend Neo Shell with additional tasks and utilities based on developer feedback. 

Neo WorkNet was developed based on community feedback and our own experience to fill a need that exists between privatenets, testnets and mainnets. There are four sets of capabilities that are packaged into the *new* New WorkNet.

First, Neo WorkNet is designed to address the specific needs of teams of developers, from projects through to large organizations. We have in the backlog integrations for bug/issue tracking, restrospectives, sprint management etc. Second, Neo WorkNet is designed to mimic a point-in-time state of a testnet/mainnet, and enables the cloning of a specific instance; and which is expected to be eventually discarded and/or superceded by a newer point-in-time state. Third, Neo WorkNet is designed to enable an enhanced CI/CD developer experience. Lastly, similar to the Neo Shell, Neo Worknet is also architected with extensibility at its core, and is designed to be extended with plug-ins, to add newer capabilities over time.

Neo Worknet enables a developer to create and run a Neo N3 consensus node that branches from a public Neo N3 
blockchain - including the official Neo N3 Mainnet and T5 Testnet. This provides the developer a local environment that
mirrors the state of a known public network at a branch point. Changes to the local branch of the network are independent 
of the public network.

## Requirements

Neo Shell and Neo-Worknet require [version 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) of
[the .NET developer platform](https://dot.net) to be installed. 

### Additional Neo-Worknet Requirements

Neo-Worknet has additional platform-specific requirements beyond .NET 6 on Ubuntu and macOS.

> Note, these are the same additional requirements that Neo-Express has. If you already are running Neo-Express, 
> Neo-Worknet will also run fine.

#### Ubuntu Installation

Installing Neo-Worknet on Ubuntu requires installing libsnappy-dev, libc6-dev and librocksdb-dev via apt-get

``` shell
sudo apt install libsnappy-dev libc6-dev librocksdb-dev -y
```

#### MacOS Installation

Installing Neo-Worknet on MacOS requires installing rocksdb via [Homebrew](https://brew.sh/)

``` shell
brew install rocksdb
```

## Installation

Neo Shell and Neo-worknet are distributed as [.NET Tools](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools).
.NET tools are [NuGet](https://nuget.org) packages containing console applications that can be installed on a developer's
machine via the `dotnet tool` command.

To install the latest version of these tools globally on youre developer machine, use the `dotnet tool install` command
in a terminal window.

``` shell
dotnet tool install Neo.Shell -g --prerelease
dotnet tool install Neo.WorkNet -g --prerelease
```

> Note, while these tools are in preview, the `--prerelease` option for `dotnet tool install` is required. 

To update these tools to the latest version, run the `dotnet tool update`
command in a terminal window.

``` shell
dotnet tool update Neo.Shell -g --prerelease
dotnet tool update Neo.WorkNet -g --prerelease
```

.NET tools also support "local tool" installation. This allows for different versions of a .NET tool to be installed in
different directories. Full details on installing and updating .NET tools are available in the
[official documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools).

## Installation (Preview)

The Neo Blockchain Toolkit has a public [package feed](https://dev.azure.com/ngdenterprise/Build/_artifacts).
that contains interim builds of Neo Shell and Worknet. You can unreleased preview builds of Neo-Shell by using the 
`--add-source` option to specify the Neo Blockchain Toolkit package feed.

For example, to update to the latest main branch version of Neo-Shell, you would run this command:

``` shell
dotnet tool update Neo.Shell -g --add-source https://pkgs.dev.azure.com/ngdenterprise/Build/_packaging/public/nuget/v3/index.json --prerelease
```

You can also specify specific versions of these tools to install by using the `--version` command line options.
For more details, please see the [official dotnet tool documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools#install-a-specific-tool-version).

If you regularly use unreleased versions of these tools in a given project, you can specify the Neo Blockchain Toolkit 
package feed in a 
[NuGet.config file](https://docs.microsoft.com/en-us/nuget/consume-packages/configuring-nuget-behavior#changing-config-settings).
Several Neo sample projects like 
[NeoContributorToken](https://github.com/ngdenterprise/neo-contrib-token)
use a NuGet.config file.

## Extending NEO Shell

NEO Shell allows developers to extend its functionality by adding custom commands. To do this, create a ~/.neo/extensions.json file that contains a list of commands executable from the shell. NEO Shell communicates with extensions using standard input/output.

There are two types of extensions.

1. NEO shell handles connections to the network. All commands are available through NEO shell. The following is an example of a `~/.neo/extensions.json` file that adds all `worknet` commands to the shell. The only requirement is that the command needs to implement an --Input parameter. This parameter is used to pass the network connection information to the command. "mapsToCommand" value can be a full path to the executable.

```json
[
    {
        "name": "NEO Worknet",
        "command": "worknet",
        "mapsToCommand": "neo-worknet"
    }
]
```  

```json
[
    {
        "name": "NEO Worknet",
        "command": "worknet",
        "mapsToCommand": "neo-worknet"
    }
]
```

An example command looks like this: `neosh neo-worknet storage get 0x5423fc51fea5ac443759323bbbccdc922cd3311c 0x17F9075AE0136F96FA4EE537CE667989A88DE65A1C31373031`

2. In addition to handling connections to the network, NEO shell can also invoke smart contracts on behalf of the commands. This is done by adding the `invokeContract` and `safe` parameters to the extension. The `invokeContract` parameter is used to indicate that the command will invoke a smart contract. The `safe` parameter is used to indicate that the command will not change the state of the blockchain. The following is an example of a `~/.neo/extensions.json` file that adds all `nft` commands to the shell. The `nft` command has two commands that can be invoked. The `transfer` command will change the state of the blockchain. The `ownerOf` command will not change the state of the blockchain.

```json
[
    {
        "name": "NEO NFT",
        "command": "nft",
        "mapsToCommand": "neonft", 
        "commands": [
            {
                "command": "transfer",
                "invokeContract": true,
                "safe": false
            },
            {
                "command": "ownerOf",
                "invokeContract": true,
                "safe": true
            }
        ]
    }
]
```

The extension commands are required to pass unsigned scripts to the NEO shell through standard out. The NEO shell will sign the scripts, execute the contract and output the result through standard out. The following is an example of a `neonft` command that will transfer an NFT from one address to another. The following snippet from the [NeoNFT] project shows how to pass the unsigned script to the NEO shell.

```csharp
...
var script = contractHash.MakeScript("transfer", toHash, idBytes, string.Empty);
var payload = new { Script = Convert.ToBase64String(script), Account = this.Account, Trace = this.Trace, Json = this.Json };
Console.WriteLine(JsonConvert.SerializeObject(payload));
```
