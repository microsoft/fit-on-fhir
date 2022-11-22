# Google Fit on FHIR Infrastructure Deployment

## Deployment Options: Bicep and ARM

There are two methods of deployment available: ARM template and Bicep template. The Bicep template is used to generate the ARM template via the azure cli. Example:

```sh
az bicep build -f azuredeploy.bicep
```

## Infrastructure: Resource List

TODO: Insert links to Microsoft docs for these resources

The following resources are deployed by the Bicep/ARM templates contained in the `deploy/` directory:

### Function Apps

Note: More detail on the following functions can be found in the /src directory README [here](./src/README.md).

* Identity Function: Authenticates users and authorizes retrieval of the user's data from Google Fit
* Sync Event Function: Periodically runs and retrieves user IDs for data to sync
* Publish Data Function: Pulls data from Google Fit and syncs/publishes it to FHIR server

### Key Vaults

* Users Key Vault: Stores refresh tokens for users who have authenticated to the Google Fit API via the web app
* Infrastructure Key Vault: Stores secrets and connection strings needed for infrastructure deployment
    * Key Vault Secret: Queue Connection String for connecting the PublishData function to the queue

### Storage Resources

* Storage Account: Contains the following sub-resources that enable data sync with FHIR
    * Blob Service: (TODO: What is this used for?)
    * Queue Service: Contains the Sync Event queue
        * Queue: Receives messages from the Sync Event Generator function.  Each message contains a userId for data that needs to be retrieved from Google Fit and pushed to FHIR
    * Table Service: Contains the User Table
        * Table: Stores a list of user IDs for users that have authenticated to Google Fit and authorized their data to be synced to FHIR server

### IoT Connector

TODO: @Justin can you fill out this section with the infra components we're provisioning for the connector?

* Event Hub (namespace, hub, consumer group)
* Other resources for the connector
* FHIR server - do we need to include anything on this?

### Monitoring

* App Insights: Collects service logs and metrics from the Azure Functions (Note: Only performance and diagnostic data is collected.  No PII/PHI is logged in the solution implemented in this repo)
* Log Analytics Workspace: Receives service logs and metrics from App Insights

## Deployment Prerequisites

* Azure subscription
* Resource Group
* Azure CLI [install](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
* [.NET 5.0 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
* [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet/3.1)

Note: To verify that you have both versions of the .NET SDK installed, run the command `dotnet --list-sdks`.

## Template Parameters

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

## Google Cloud Platform Setup

For information on how to setup a Google Cloud Platform account, refer to the README [here](./src/GoogleFitOnFhir.Identity/README.md).
