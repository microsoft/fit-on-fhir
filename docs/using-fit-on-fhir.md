# Using Fit on FHIR

Congratulations. You have deployed Fit on FHIR to your Azure subscription and are ready to start collecting Google Fit Data. The first thing to do is have users on-board by authorizing sharing fitness data.

## User Authorization Endpoint

When users navigate to the authorize endpoint, they are redirected to a Google sign-in page where they can enter their credentials and opt-in to sharing data with your Fit on FHIR instance. After they complete the authorization process, Fit on FHIR will continuously import the fitness data they opted to share. By default, data imports occur once per hour.

Your authorization endpoint is hosted by the Authorization Function. The endpoint to begin the authorization flow can be determined by viewing the Function overview page, finding the Function base URL and appending **api/googlefit/authorize** to the base.

## Authorization Function Response Data

To allow for users to be redirected to a Google sign-in page, the Authorization function returns a JSON object that contains two values:

1. **AuthUrl** - This is the URL that should be used to navigate the user to the sign-in page.
1. **ExpiresAt** - This is a timestamp that indicates when authorized access will expire, for the user that is trying to complete the authorization process.  Attempting to complete the process after this time will result in a 401 unauthorized response.

## Finding your Authorization Function Base URL

In your resource group, find the Authorization Function. It will be named **authorize-{YOUR_BASE_NAME}**, where, YOUR_BASE_NAME is the basename parameter provided when the resources were deployed.
![Auth Function in Resource Group](../media/auth-function-resource-group.png)

In the overview section of the Function copy the base URL. Append the base URL with **api/googlefit/authorize**.
![Auth Function Base URL](../media/auth-function-url.png)

## Configuring Access to your Authorization Function

Access to the Authorization function can be configured to either allow for anonymous login, or login with authentication.  This setting is configured by setting
*AuthenticationConfiguration__IsAnonymousLoginEnabled* to true for anonymous logins, and false for login with authentication.  This can be found in can be found in Settings->Configuration.

**Required Query Parameters**:

Anonymous login requests require two query parameters, *external-id* and *external-system*.  *external-system* represents the name of the medical record system in which the user's current health record
resides.  *external-id* represents the identifier for the user's health record in *external-system*.

Login with authentication requests require passing a valid OAuth2 access token, and one query parameter *redirect-url*.  *redirect-url* represents the URL that
the Authorization function will redirect to, once authorization with Google is complete.  The URL contained in *redirect-url* must match a URL that is on the
approved list for the Authorization function.  The approved list of redirect URLs can be found in Settings->Configuration and is labeled *AuthenticationConfiguration__RedirectUrls*.
*external-id* and *external-system* query parameters are not allowed when login with authentication is enabled.  Including either of these in a request will result in
a Bad Request (400) response.

**Optional Query Parameters**:

When using login with authentication, an optional *state* query parameter can be provided.  *state* can be used to enter any info that should be passed along in
the request made to the *redirect-url* when authorization with Google is complete.
