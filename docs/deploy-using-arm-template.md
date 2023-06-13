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
|**google_client_id**|**The Google OAuth2 web application client id.** *[Where to find your Google Client Id and Secret](./finding-google-client-id-and-secret.md)*|true
|**google_client_secret**|**The Google OAuth2 web application secret.** [Where to find your Google Client Id and Secret](./finding-google-client-id-and-secret.md)*|true
|**repository_url**|**The repository where the fit-on-fhir source code resides.** *The default value is '[https://github.com/Microsoft/fit-on-fhir]()'*|false
|**repository_branch**|**The source code branch to be deployed.** *The default value is 'main'*|false
|**google_dataset_request_limit**|**The maximum Google Fit data points returned per dataset request.** *The default value is 1000*|false
|**google_max_concurrency**|**The maximum concurrent tasks allowed per Google Fit dataset request.** *The default value is 10*|false
|**google_fit_scopes**|**The Google Fit data authorization scopes allowed for users of this service (see [https://developers.google.com/fit/datatypes#authorization_scopes](https://developers.google.com/fit/datatypes#authorization_scopes) for more info).** *Defaults to all available scopes*|false
|**google_max_requests_per_minute**|**The maximum number of requests that can be made to the Google APIs in a one minute period.** *Defaults to 300*|false
|**google_historical_import_time_span**|**The time period in days, hours, minutes, and seconds from now into the past, that the first data import will cover.** *Defaults to 30 days*|false
|**authentication_anonymous_login_enabled**|Enables anonymous logins (true) or requires authentication (false). *Defaults to false*|false
|**authentication_identity_providers**|A list of identity provider URLs used when authentication is required.|false
|**authentication_audience**|The URL that any access tokens are granted for.|false
|**authentication_blob_container_name**|Name for the authentication data storage container.|false
|**authentication_redirect_urls**|A comma delimited list of approved redirect URLs that can be navigated to when authentication completes successfully.|false

## Provision using Azure CLI (Bicep or ARM)

You can use the [azuredeploy.parameters.json](../deploy/azuredeploy.parameters.json) file to supply parameters listed above or you can manually supply the parameters using key-value pairs. Documentation about the az deployment group create command can be found [here](https://docs.microsoft.com/en-us/cli/azure/deployment/group?view=azure-cli-latest#az-deployment-group-create).

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

Once the deployment has completed, if the Authorized Redirect URI has not been set be sure to configure it. Instructions can be found [here](./setting-redirect-uris.md).
