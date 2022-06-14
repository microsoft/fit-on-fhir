# Deploying the solution via ARM Template

This guide details provisioning and configuring a working Fit on FHIR solution to your Azure Subscription. The provided [ARM Template](../deploy/azuredeploy.json) is the easiest way to provision all the required resources and ensure they are configured correctly.

## Provision using the Azure Portal

You can start the deploying to your Azure subscription by simply clicking the button below.

<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FMicrosoft%2Ffit-on-fhir%2Fmain%2Fdeploy%2Fazuredeploy.json" target="_blank">
    <img src="../media/deploy-button.png"/>
</a>

### Prerequisites
To run this ARM template the following additional items must be set up before execution:

### Parameters
You must provide the following parameters when deploying the ARM Template.

|Parameter|Use|Required
|---|---|---
|**basename**|**A name that is used to name provisioned resources.** *Should be alphanumeric and less than 16 characters. Must be globally unique.*|true
|**location**|**The location where the resources(s) are deployed.** *Choose the Azure region that's right for you and your customers. Not every resource is available in every region. The default value will be the location defined for the resource group.*|false
|**google_client_id**|**The Google OAuth2 web application client id.**|true
|**google_client_secret**|**The Google OAuth2 web application secret.**|true
|**repository_url**|**The repository where the fit-on-fhir source code resides.** *The default value is 'https://github.com/Microsoft/fit-on-fhir'*|false
|**repository_branch**|**The source code branch to be deployed.** *The default value is 'main'*|false

## Provision using Azure CLI (Bicep or ARM)

You can use the [azuredeploy.parameters.json](../deploy/azuredeploy.parameters.json) file to supply parameters listed above or you can manually supply the parameters using key-value pairs. Documentation about the az deployment group create command can be founde [here](https://docs.microsoft.com/en-us/cli/azure/deployment/group?view=azure-cli-latest#az-deployment-group-create).

### Using the azuredeploy.parameters.json file
```sh
az deployment group create \
    --name deployment01 \
    --resource-group {YOUR_RESOURCE_GROUP_NAME} \
    --template-file azuredeploy.bicep \
    --parameters @azuredeploy.parameters.json \
```

or

### Using key-value pairs
```sh
az deployment group create \
    --name deployment01 \
    --resource-group {YOUR_RESOURCE_GROUP_NAME} \
    --template-file azuredeploy.bicep \
    --parameters basename={YOUR_BASE_NAME}
    --parameters google_client_id={YOUR_CLIENT_ID}
    --parameters google_client_secret={YOUR_CLIENT_SECRET}
```

NOTE: To deploy as an ARM template instead of a Bicep template, repeat the command above replacing the following argument `--template-file azuredeploy.json`.