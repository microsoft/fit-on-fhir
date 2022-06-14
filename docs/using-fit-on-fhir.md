# Using Fit on FHIR

Congratulations. You have deployed Fit on FHIR to your Azure subscription and are ready to start collecting Google Fit Data. The first thing to do is have users on-board by authorizing sharing fitness data.

## User authorization endpoint

When users navigate to the authorize endpoint, they are redirected to a Google sign-in page where they can enter their credentials and opt-in to sharing data with your Fit on FHIR instance. After they complete the authorization process, Fit on FHIR will continuously import the fitness data they opted to share. By default, data imports occur once per hour.

**Your users can authorize sharing Google Fit data by navigating to *https://authorize-{YOUR_BASE_NAME}.azurewebsites.net/api/googlefit/authorize***

Your authorization endpoint is hosted by the Authorization Function. The endpoint to begin the authorization flow can be determined by viewing the Function overview page, finding the Function base URL and appending **api/googlefit/authorize** to the base.

## Finding your Authorization Function Base URL

In your resource group, find the Authorization Function. It will be named **authorize-{YOUR_BASE_NAME}**, where, YOUR_BASE_NAME is the basename parameter provided when the resources were deployed. 
![Auth Function in Resource Group](../media/auth-function-resource-group.png)

In the overview section of the Function copy the base URL. Append the base URL with **api/googlefit/authorize**.
![Auth Function Base URL](../media/auth-function-url.png)