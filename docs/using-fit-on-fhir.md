# Using Fit on FHIR

Congratulations. You have deployed Fit on FHIR to your Azure subscription and are ready to start collecting Google Fit Data. The first thing to do is have users on-board by authorizing sharing fitness data.

## User Authorization Endpoint

When users navigate to the authorize endpoint, they are redirected to a Google sign-in page where they can enter their credentials and opt-in to sharing data with your Fit on FHIR instance. After they complete the authorization process, Fit on FHIR will continuously import the fitness data they opted to share. By default, data imports occur once per hour.

Your authorization endpoint is hosted by the Authorization Function. The endpoint to begin the authorization flow can be determined by viewing the Function overview page, finding the Function base URL and appending **api/googlefit/authorize** to the base.

## Authorization Function Response Data

To allow for users to be redirected to a Google sign-in page, the Authorization function returns a JSON object that contains two values:

1. **AuthUrl** - This is the URL that should be used to navigate the user to the sign-in page.
1. **ExpiresAt** - This is a timestamp that indicates when authorized access will expire, for the user that is trying to complete the authorization process.

## Finding your Authorization Function Base URL

In your resource group, find the Authorization Function. It will be named **authorize-{YOUR_BASE_NAME}**, where, YOUR_BASE_NAME is the basename parameter provided when the resources were deployed.
![Auth Function in Resource Group](../media/auth-function-resource-group.png)

In the overview section of the Function copy the base URL. Append the base URL with **api/googlefit/authorize**.
![Auth Function Base URL](../media/auth-function-url.png)

## Configuring Access to your Authorization Function

Access to the Authorization function can be configured to either allow for anonymous login, or login with authentication.  This setting is configured by setting
*AuthenticationConfiguration__IsAnonymousLoginEnabled* to true for anonymous logins, and false for login with authentication.  This can be found in can be found in Settings->Configuration.

**Required Query Parameters**:

Anonymous login requests require two query parameters, *external_id* and *external_system*.  The *external_id* and *external_system* are used to create a FHIR [Identifier](http://hl7.org/fhir/datatypes.html#Identifier) that is stored in the [Patient Resource](http://hl7.org/fhir/patient.html). This Identifier can be used to link the Patient Resource to a user (or patient) in a different system.

_An example anonymous login request might look like: https://authorize-fitonfhir.azurewebsites.net/api/googlefit/authorize?external_id=externalPatientA&external_system=externalSystem&redirect_url=https://www.microsoft.com/_

Login with authentication requests require passing a valid OAuth2 access token, and one query parameter *redirect_url*.  *redirect_url* represents the URL that
the Authorization function will redirect to, once authorization with Google is complete.  The URL contained in *redirect_url* must match a URL that is on the
approved list for the Authorization function.  The approved list of redirect URLs can be found in Settings->Configuration and is labeled *AuthenticationConfiguration__RedirectUrls*.
*external_id* and *external_system* query parameters are not allowed when login with authentication is enabled.  Including either of these in a request will result in
a Bad Request (400) response.

In addition to declaring the approved list of redirect URLs, it is also necessary to configure the *Audience* and *Identity Providers* that will be used during the authentication process.
The *Audience*, which is equivalent to the aud claim in the OAuth2 access token, can be declared by setting the value for *AuthenticationConfiguration__Audience* in Settings->Configuration.
The *Identity Providers*, which are the equivalent to the iss claim in the OAuth2 access token, can be declared by setting the value for *AuthenticationConfiguration__IdentityProviders* in Settings->Configuration.
*AuthenticationConfiguration__IdentityProviders* is a list, so it is possible to declare multiple providers if necessary, separating each provider by a comma.

_An example authenticated login request might look like: https://authorize-fitonfhir.azurewebsites.net/api/googlefit/authorize?redirect_url=https://www.microsoft.com/ with the Bearer header set to a valid OAuth2 access token_

**Optional Query Parameters**:

When using login with authentication, an optional *state* query parameter can be provided.  *state* can be used to enter any info that should be passed along in
the request made to the *redirect-url* when authorization with Google is complete.

_An example authenticated login request with this optional parameter might look like: https://authorize-fitonfhir.azurewebsites.net/api/googlefit/authorize?redirect_url=https://www.microsoft.com/&state=yourStateValue again with the Bearer header set to a valid OAuth2 access token_
