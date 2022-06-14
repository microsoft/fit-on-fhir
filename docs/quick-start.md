# Quick start

This guide provides step-by-step instructions on how to deploy a working fit-on-fhir solution to your Azure Subscription.

## Prerequisites

1. **Azure Subscription** - Make sure you have an active Azure Subscription. If you don't you can create a free Azure account [here](https://azure.microsoft.com/en-us/free/).
1. **Google Account** - If you have an existing gmail account, you can use it, or you can create a new account [here](https://accounts.google.com).

## Setup Google APIs and OAuth Client Credentials

The easiest way to get your Google APIs setup and get OAuth2 configured is to follow the [Google Cloud Platform Setup guide](google-setup.md). The guide will walk you through each step required to configure your Google APIs for use with the fit-on-fhir project.

## Deploy to Azure

Once you have setup your Google APIs and OAuth Client Credentials, you can deploy the Fit on FHIR solution to your Azure subscription by using the provided ARM template. Follow [this guide](deploy-using-arm-template.md) to deploy using the Azure Portal or Azure CLI.

## Authorize test users

If you did not add test users when following the guide to setting up Google APIs and OAuth Client Credentials, or you want to add additional test users, you can follow this [guide](add-test-users.md).

## On board users

With your Google APIs and OAuth credentials setup, the Azure resources deployed and test users added, Fit on FHIR should be ready to collect data from Google Fit users and persist the data as FHIR Observations. To on-board a user, have them navigate to the authorize API. Complete instructions can be found [here](using-fit-on-fhir.md#user-authorization-endpoint).
