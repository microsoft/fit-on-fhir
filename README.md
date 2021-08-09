# Google FIT on FHIR

## Introduction

TODO: Expand introduction with a little more detail/context

This repo is available to help you get started with bringing Google FIT to FHIR. The webapp will help to authenticate your patients with the Google FIT API and the Azure Function that continously publishes patient data as it becomes available.

## Prerequisites

* [.NET 5.0 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
* [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet/3.1)
* ...TODO: Other prerequisites

Note: To verify that you have both versions of the .NET SDK installed, run the command `dotnet --list-sdks`.

## Architecture

### Architecture Diagram
The following is a visual representation of the architecture that gets deployed through this solution:

![Architecture Diagram](./media/original-architecture-diagram.png)

### Resource List

The following resources are deployed by the Bicep/ARM templates contained in the `deploy/` directory:

#### Function Apps

* Identity Function App
    * Identity Azure Function (HTTP trigger): Presents a UI for users to authenticate to Google Fit and authorize data sync.  Receives OAuth callback.  Stores refresh token in the "users" key vault, stores user ID in the "users" table in storage, and registers the user on the FHIR server.
* Sync Event Function App
    * Sync Event Azure Function (timer trigger): Periodically runs and retrieves list of user ID's to sync from the "users" table in storage.
* Publish Data Function App
    * Publish Data Azure Function (queue trigger): Retrieves messages from the "users" queue in the storage account.  Pulls user data from Google Fit and publishes it to FHIR server.  Handles update of refresh token and stores new token in key vault.

#### Key Vaults

* Users Key Vault: Stores refresh tokens for users who have authenticated to the Google Fit API via the web app
* Infrastructure Key Vault: Stores secrets and connection strings needed for infrastructure deployment
    * Key Vault Secret: Queue Connection String for connecting the PublishData function to the queue

#### Storage

* Storage Account: Contains the following sub-resources that enable data sync with FHIR
    * Blob Service: (TODO: What is this used for?)
    * Queue Service: Contains the Sync Event queue
        * Queue: Receives messages from the Sync Event Generator function.  Each messsage contains a userId for data that needs to be retrieved from Google Fit and pushed to FHIR
    * Table Service: Contains the User Table
        * Table: Stores a list of user ID's for users that have authenticated to Google Fit and authorized their data to be synced to FHIR server

#### FHIR

* Azure FHIR Service: (TODO: Description)
    * Azure FHIR IoMT connector: (TODO: Description)

#### Monitoring

* App Insights: Collects service logs and metrics from the Azure Functions (Note: Only performance and diagnostic data is collected.  No PII/PHI is logged in the solution implemented in this repo)
* Log Analytics Workspace: Receives service logs and metrics from App Insights

## Infrastructure Deployment

### Deployment Steps

TODO: Fill out this section with sample commands for deploying via Bicep/ARM

Steps:
* Set variables for input parameters
    * `basename`: Base name that is used to name provisioned resources. Should be alphanumeric and less than 16 characters.
* AZ CLI to run deployment group create using Bicep/ARM template

TODO: Question - are we assuming an existing RG?

### Test the Deployment

#### Authentication + Authorization

TODO: Steps for directing users to the Identity function UI for authentication and authorization of data sync

#### Verifying Data Sync

TODO: Steps for verifying that data sync is working correctly

## FAQ and Troubleshooting

TODO

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit [https://cla.opensource.microsoft.com](https://cla.opensource.microsoft.com).

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).

Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.
