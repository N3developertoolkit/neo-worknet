# Neo Shell Command Reference

Neo Shell is a cross-platform and extensible, unified command-line interface for managing Neo N3 chain resources on worknet
and testnet etc. The Neo Shell enables developers to execute commands through a terminal using interactive command-line 
prompts or scripts.

> Note, you can pass -?|-h|--help to show a list of supported commands or to show help information about a specific command.

## Usage

``` shell
neosh COMMAND|EXTENSION [sub-commands] [—Global flags]
```

## Specifying Signing and Non-Signing Accounts

Many of the Neo-Express commands require the user to specify account information. In some cases, this
account is used to sign a transaction that is submitted to the blockchain network. 

### Specifying a Signing Account

An account used for signing must have an accessible private key. Signing accounts can be specified in
multiple ways:

- Neo-Express or Worknet wallet nickname. Note, this includes `node1` etc to specify the default wallet account
  associated with each consensus node
- A [WIF encoded](https://developer.bitcoin.org/devguide/wallets.html#wallet-import-format-wif) private key
- A [standard NEP-2 Passphrase-protected private key](https://github.com/neo-project/proposals/blob/master/nep-2.mediawiki).
    - When using a NEP-2 protected private key, the passphrase must be specified using the `--password` option
- The path to a [standard NEP-6 JSON wallet](https://github.com/neo-project/proposals/blob/master/nep-6.mediawiki).
    - When using a NEP-6 wallet, the password must be specified using the `--password` option. 
    - Note, Neo-Express only supports NEP-6 wallets with either a single account or a single default account

NEP-2 private key and NEP-6 JSON wallet are password protected. When using one of these methods, the password
can be specified using the `--password` option. If the password is not specified on the command line, Neo-Express
will prompt the user to enter the password.

### Specifying a Non-Signing Account

A account used that is not used for signing doesn't need an accessible private key. Non-Signing accounts
can be specified in multiple ways:

- Neo-Express or Worknet wallet nickname. Note, this includes `node1` etc to specify the default wallet account
  associated with each consensus node
- A standard Neo N3 address such as `Ne4Ko2JkzjAd8q2sasXsQCLfZ7nu8Gm5vR`
- A [WIF encoded](https://developer.bitcoin.org/devguide/wallets.html#wallet-import-format-wif) private key

## Commands
```
connect       Connect to a network for example worknet or testnet
contract      Commands to manage smart contracts
show          Show information
transfer      Transfer asset between accounts
```

## neosh connect

```
Connect to a network for example worknet or testnet

Usage: neosh connect [command] [options]

Options:
  -i|--input <INPUT>  Path to neo data file
  -?|-h|--help        Show help information.

Commands:
  current             Get the current connection
```

Before using other Neo Shell commands, you must first establish a connection to the Neo N3 blockchain network you 
want to interact with via the `connect` command. Neo Shell supports connecting to any Neo N3 blockchain network that
provides the [JSON-RPC methods](https://docs.neo.org/docs/en-us/reference/rpc/latest-version/api.html) implemented by 
the [RpcServer Plugin](https://docs.neo.org/docs/en-us/node/cli/config.html#installing-plugins). This includes 
Neo-Worknet and Neo-Express. 

> Note, currently, Neo Shell only supports connecting to Neo-WorkNet and Neo-Express. Support for connecting to a public
> Neo N3 Blockchain is coming in a future update to the preview

## neosh contract 

The `contract` command has a series of subcommands for managing smart contracts on a Neo N3 blockchain network

```
Commands:
  deploy        Deploy contract
  invoke        Invoke a contract using parameters from .neo-invoke.json file
  list          List deployed contracts
  run           Invoke a contract using parameters passed on command line
  update        Update a contract
```

### neosh contract deploy

```
Usage: neosh contract deploy [options] <Contract> <Account>

Arguments:
  Contract                            Path to contract .nef file
  Account                             Account to pay contract deployment GAS fee. Can be a name or a WIF string.

Options:
  -w|--witness-scope <WITNESS_SCOPE>  Witness Scope to use for transaction signer (Default: CalledByEntry)
                                      Allowed values are: None, CalledByEntry, Global.
                                      Default value is: CalledByEntry.
  -d|--data <DATA>                    Optional data parameter to pass to _deploy operation
  -p|--password <PASSWORD>            Password to use for NEP-2/NEP-6 account
  -i|--input <INPUT>                  Path to the data file
  -t|--trace                          Enable contract execution tracing
  -f|--force                          Deploy contract regardless of name conflict
  -j|--json                           Output as JSON
  -?|-h|--help                        Show help information.
```

The `contract deploy` command deploys a smart contract to a Neo N3 blockchain. The command takes
a path to an .NEF file generated by a Neo contract compiler like 
[NCCS compiler for .NET](https://github.com/neo-project/neo-devpack-dotnet).
Additionally, the command requires the signing account that will pay the GAS deployment fee.

### neosh contract update

Update a contract

```
Usage: neosh contract update [options] <Contract> <NefFile> <Account>

Arguments:
  Contract                            Contract name or invocation hash
  NefFile                             Path to contract .nef file
  Account                             Account to pay contract deployment GAS fee. Can be a name or a WIF string

Options:
  -w|--witness-scope <WITNESS_SCOPE>  Witness Scope to use for transaction signer (Default: CalledByEntry)
                                      Allowed values are: None, CalledByEntry, Global.
                                      Default value is: CalledByEntry.
  -d|--data <DATA>                    Optional data parameter to pass to _deploy operation
  -p|--password <PASSWORD>            Password to use for NEP-2/NEP-6 account
  -i|--input <INPUT>                  Path to the data file
  -t|--trace                          Enable contract execution tracing
  -f|--force                          Deploy contract regardless of name conflict
  -j|--json                           Output as JSON
  -?|-h|--help                        Show help information.
```

Note, this command assumes the contract specified as a public `update` method taking two parameters. If your contract
has an update method with different name or parameters, you have to use `contract invoke` instead.

### neosh contract invoke
Invoke a contract using parameters from .neo-invoke.json file

```
Usage: neosh contract invoke [options] <InvocationFile> <Account>

Arguments:
  InvocationFile                      Path to contract invocation JSON file
  Account                             Account to pay contract invocation GAS fee

Options:
  -w|--witness-scope <WITNESS_SCOPE>  Witness Scope to use for transaction signer (Default: CalledByEntry)
                                      Allowed values are: None, CalledByEntry, Global.
                                      Default value is: CalledByEntry.
  -r|--results                        Invoke contract for results (does not cost GAS)
  -g|--gas                            Additional GAS to apply to the contract invocation
                                      Default value is: 0.
  -p|--password <PASSWORD>            password to use for NEP-2/NEP-6 account
  -t|--trace                          Enable contract execution tracing
  -j|--json                           Output as JSON
  -i|--input <INPUT>                  Path to the data file
  -?|-h|--help                        Show help information.
```

The `contract invoke` command generates a script from an
[invocation file](https://github.com/ngdenterprise/design-notes/blob/master/NDX-DN12%20-%20Neo%20Express%20Invoke%20Files.md)
and submits it to the Neo N3 blockchain network as a transaction.

A script can be invoked either for results (specified via the `--results` option) or to make changes
(specified via the signed account argument). If a script is submitted for results, it may read information
stored in the blockchain, but any changes made to blockchain data will not be saved. If a submitted
for changes, a signed account must be specified and any results returned by the script will not be available 
immediately. For scripts submitted for changes, a transaction ID is returned and the execution results can 
be retrieved via the `show transaction` command (described below).

### neoxp contract run

Invoke a contract using parameters passed on command line

```
Usage: neosh contract run [options] <Contract> <Method> <Arguments>

Arguments:
  Contract                            Contract name or invocation hash
  Method                              Contract method to invoke
  Arguments                           Arguments to pass to the invoked method

Options:
  -a|--account <ACCOUNT>              Account to pay contract invocation GAS fee
  -w|--witness-scope <WITNESS_SCOPE>  Witness Scope to use for transaction signer (Default: CalledByEntry)
                                      Allowed values are: None, CalledByEntry, Global.
                                      Default value is: CalledByEntry.
  -r|--results                        Invoke contract for results (does not cost GAS)
  -g|--gas                            Additional GAS to apply to the contract invocation
                                      Default value is: 0.
  -p|--password <PASSWORD>            password to use for NEP-2/NEP-6 account
  -t|--trace                          Enable contract execution tracing
  -j|--json                           Output as JSON
  -i|--input <INPUT>                  Path to the data file
  -?|-h|--help                        Show help information.
```

Like `contract invoke`, the `contract run` command generates a script and submits it to the Neo N3
blockchain network as a transaction wither for results or changes. However, unlike `contract invoke`, 
the `contract run` command generates the script from command line parameters instead of an invocation
file. The command line constraints limit the flexibility of `contract run` relative to `contract invoke`,
but saves the developer from needing to create an invocation file for simple contract invocation scenarios.

Instead of a path to an invocation file, The `contract run` command takes arguments specifying the contract
(either by name or hash) and the method to invoke, plus zero or more contract arguments. These contract
arguments are string encoded values, following similar rules to 
[string arguments in an invocation file](https://github.com/ngdenterprise/design-notes/blob/master/NDX-DN12%20-%20Neo%20Express%20Invoke%20Files.md#args-property).

### neosh contract list

List deployed contracts

```
Usage: neosh contract list [options]

Options:
  -i|--input <INPUT>  Path to the data file
  -j|--json           Output as JSON
  -?|-h|--help        Show help information.
```

The `contract list` command writes out the name and contract hash of every contract deployed in a
Neo N3blockchain network. This includes native contracts that are part of the core Neo platform.

## neosh show

The `show` command will display information from the connected N3 blockchain. There are multiple subcommands 
representing the different  information that is available:

- `show balance` will display the balance of a single NEP-17 asset (including NEO and GAS) of a specific account
- `show block` with display the contents of a single block, specified by index or hash
- `show transaction` with display the contents of a transaction specified by hash and its execution results if available
  - `show tx` is an alias for `show transaction`

## neoxp transfer

Transfer an NEP-17 asset between accounts

```Usage: neosh transfer [options] <Quantity> <Asset> <Sender> <Receiver>

Arguments:
  Quantity                  Amount to transfer
  Asset                     Asset to transfer (symbol or script hash)
  Sender                    Account to send asset from
  Receiver                  Account to send asset to

Options:
  -d|--data <DATA>          Optional data parameter to pass to transfer operation
  -p|--password <PASSWORD>  password to use for NEP-2/NEP-6 sender
  -i|--input <INPUT>        Path to neo-express data file
  -t|--trace                Enable contract execution tracing
  -j|--json                 Output as JSON
  -?|-h|--help              Show help information.
```

The `transfer` command is used to transfer NEP-17 assets between accounts in a Neo N3
blockchain network. The transfer command has four required arguments

- the quantity to transfer as an integer or `all` to transfer all assets of the specified type 
- The asset to transfer. This can be specified as contract hash or
  [NEP-17](https://github.com/neo-project/proposals/blob/master/nep-17.mediawiki)
  token symbol such as `neo` or `gas`
- Signing account that is sending the asset
- Non-signing account that is receiving the asset
