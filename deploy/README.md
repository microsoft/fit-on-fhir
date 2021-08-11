# Google Fit on FHIR Infrastructure Deployment

There are two methods of deployment available: ARM template and Bicep template. The Bicep template is used to generate the ARM template via the azure cli. Example:

```sh
az bicep build -f azuredeploy.bicep
```

## Prerequisites

- Azure subscription
- Resource Group
- Azure CLI [install](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [.NET 5.0 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
- [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet/3.1)

Note: To verify that you have both versions of the .NET SDK installed, run the command `dotnet --list-sdks`.

## Parameters

The following parameters can be supplied to the `az deployment group create` command using the `--parameters` argument.

| name  | description   |
|-------|---------------|
| basename | Base name that is used to name provisioned resources. Should be alphanumeric and less than 16 characters. |

## Command Line Deployment

You can use the `azuredeploy.parameters.json` file to supply parameters or you can manually supply parameters using key-value pairs.

```sh
az deployment group create \
    --name rollout01 \
    --resource-group rg-googlefitonfhir \
    --template-file azuredeploy.bicep \
    --parameters @azuredeploy.parameters.json \
    --parameters basename=googlefitonfhir
```

NOTE: To deploy as an ARM template instead of a Bicep template, repeat the command above replacing the following argment `--template-file azuredeploy.json`.

## Resources

The following resources will be provisioned in Azure.

| type      | purpose   |
|-----------|-----------|
| keyvault  | used to store user authentication tokens |
