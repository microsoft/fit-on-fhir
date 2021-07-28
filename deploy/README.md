# Infrastructure Deployment

There are two methods of deployment available: ARM template and Bicep template. The Bicep template is used to generate the ARM template via the azure cli. Example:

```script
az bicep build -f azuredeploy.bicep
```

## Pre-requisites

- Azure subscription
- Resource Group
- Azure CLI [install](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)

## Parameters

The following parameters can be supplied to the `az deployment group create` command using the `--parameters` argument.

| name  | description   |
|-------|---------------|
| basename | Base name that is used to name provisioned resources. Should be alphanumeric and less than 16 characters. |

## Command Line Deployment

You can use the `azuredeploy.parameters.json` file to supply parameters or you can manually supply parameters using key-value pairs.

```script
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
