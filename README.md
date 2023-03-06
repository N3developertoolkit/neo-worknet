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
