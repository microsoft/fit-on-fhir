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

* Dequeues the message
* Reads the user ID from the message
* Retrieves the user's refresh token from key vault
* Retrieves new access token with refresh token
* Pulls the user's data from the Google Fit API with the access token
* Transforms data to be compatible with IoT connector
* Pushes the data to the IoT connector for FHIR server
* Updates the user's refresh token and stores the new token in key vault (incomplete)

## E2E Tests

* Deploy ephemeral infrastructure
* Deploy durable infrastructure
* Copy e2e test google account refresh token from durable key vault to ephemeral key vault
* Get new access token with refresh token
* Create Google Fitness datasource for e2e test account
* Insert dataset for 25 days ago into datasource
* --- incomplete below this line ---
* Wait for sync/publish (should migrate last month)
* Validate data in FHIR
* Insert dataset for now
* Wait for sync/publish (should migrate recent interval)
* Validate data in FHIR
* Delete datasource on e2e test account (complete)
* Tear down infrastructure (complete)
