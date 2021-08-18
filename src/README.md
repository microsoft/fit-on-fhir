# Google FIT on FHIR Functions

## Identity Function (HTTP Trigger)

The main entry point for the Fit to FHIR application, the identity function responds to an HTTP web request and does the following:

* Presents a UI to patients with a button allowing them to authorize Google Fit data sync with FHIR server
* Authenticates the user with the Google Fit API via OAuth
* Receives the OAuth callback
    * Stores the refresh token in the "users" key vault
    * Stores the user ID in the "users" storage table

## Sync Event Function (Timer Trigger)

The Sync Event function runs on a timer at regular intervals, and does the following:

* Retrieves a list of user IDs from the "users" storage table for data that needs to be synced to FHIR
* Creates a queue message for each user ID in the "users" queue in storage

## Publish Data Function (Queue Trigger)

The Publish Data function is triggered by new messages added to the users queue in storage (by the Sync Event function described above), and does the following:

* Dequeues the message (TODO: Verify that it does this)
* Reads the user ID from the message
* Retrieves the user's refresh token from key vault and uses it to pull the user's data from the Google Fit API
* TODO: Assuming we transform the data to some FHIR-compatible format?
* Pushes the data to the IoT connector for FHIR server
* Updates the user's refresh token and stores the new token in key vault
