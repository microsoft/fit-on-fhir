# Infrastructure Deployment
There are two methods of deployment available: ARM template and Bicep template. The Bicep template is used to generate the ARM template via the azure cli. Example:
```
az bicep build -f azuredeploy.bicep
```

## Pre-requisites
- azure subscription
- resource group
- [azure-cli](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)

## Parameters
The following parameters can be supplied to the `az deployment group create` command using the `--parameters` argument 

//TODO: make a table
basename | Base name that is used to name provisioned resources. Should be alphanumeric and less than 16 characters.

## Command Line Deployment
```
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
