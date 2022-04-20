# Google Cloud Platform Setup

In order to test your deployment with the Google Fit API, it is necessary to setup a Google Cloud Platform account.
There are 5 areas of concern that must be addressed in order to complete this process.

## Create a Google Developer Account
The first step is to create a google developer account.  Go to the [portal](https://cloud.google.com/), and follow the steps
for signing up.

## Create a project in the dashboard
Once your developer account has been created, go to your Google Cloud Platform console.  From the Dashboard view, choose 
Create Project.  Give your project a name, and choose your location.

## Enable API access
Next you'll want to enable API access for your project.  From the Dashboard view, navigate to *Enabled APIs & services* 
from under the *APIs & Services* menu on the left side pane.  Here you will see a pre-populated list of currently enabled 
APIs for your project.  You will need to enable the *Fitness API* and *People API*.  To do this, click on the button near the top
of the view labeled **ENABLE API AND SERVICES**.  This will take you to a new page, where you can enter the API names in the search
box.  For each one, when you are on the page for the API, click the Enable button.

## Create OAuth credentials
With the APIs enabled, you will need to create your OAuth credentials in order to allow your app to authorize with Google. 
From the Dashboard view, navigate to *Credentials* from under the *APIs & Services* menu on the left side pane.  Here you will see 
a list of any current OAuth 2.0 clients.  You will need to create a new one for your project.  To do this, click on the button near the top
of the view labeled **CREATE CREDENTIALS**.  This will present you with a drop down list.  Choose **OAuth client ID**.  This will take you to 
a new page, where you must choose the application type for this client ID.  Select **Web application**.  Before exiting this page, see the
next step below.

## Add javascript origin URLs
Lastly, you need configure the base URI and callback URI for your project.  Click the button labeled **Add URI** under *Authorized JavaScript origins*
to add the base URI.  Click the button labeled **Add URI** under *Authorized redirect URIs* to add the callback URI.  



